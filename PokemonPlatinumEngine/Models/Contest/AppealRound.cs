using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Contest;

/// <summary>Hoenn's four ranks, in Emerald's order (<c>CONTEST_RANK_NORMAL</c> to <c>MASTER</c>).</summary>
public enum AppealRank { Normal, Super, Hyper, Master }

/// <summary>A move a contestant can appeal with: the move and Emerald's data for it on the stage.</summary>
public sealed record AppealMove(MoveData Move, HoennContestMove Contest)
{
    public string Name => Move.Name;

    public static AppealMove Of(MoveData move) => new(move, move.HoennContest);
}

/// <summary>One of the four on the stage: the player's Pokémon or one of Emerald's contestants.</summary>
public sealed class Appealer
{
    public string Trainer { get; init; } = "";
    public Pokemon Pokemon { get; init; } = null!;
    public bool IsPlayer { get; init; }

    /// <summary>Up to four moves, in the order they are shown.</summary>
    public IReadOnlyList<AppealMove> Moves { get; init; } = Array.Empty<AppealMove>();

    /// <summary>Cool, beauty, cute, smart and tough (the scarf's twenty added, held to 255) and the sheen.</summary>
    public int[] Stats { get; init; } = new int[5];
    public int Sheen { get; init; }

    /// <summary>The first round's points: the condition of the contest's quality (<c>CalculateContestantRound1Points</c>).</summary>
    public int Round1Points { get; set; }

    /// <summary>The Emerald contestant this is, for an opponent.</summary>
    public HoennContestOpponent? Source { get; init; }
}

/// <summary>Whether a contestant's condition just rose, was spent, or is as it was (<c>CONDITION_GAIN</c> and its fellows).</summary>
public enum ConditionChange { None, Gain, Lose }

/// <summary>
/// What a contestant is, in the middle of a contest (the original's <c>ContestantStatus</c>): everything that lasts a
/// round, a few appeals or all five.
/// </summary>
public sealed class AppealStatus
{
    /// <summary>The hearts of this appeal, in points (ten to a heart), and what the move was worth by itself.</summary>
    public int Appeal { get; internal set; }
    public int BaseAppeal { get; internal set; }

    /// <summary>The points the contestant has won over the appeals so far.</summary>
    public int PointTotal { get; internal set; }

    /// <summary>0 for the best, after each round (ties share the best place).</summary>
    public int Ranking { get; internal set; }

    /// <summary>The condition in tens of points, 0 to 30 (a star for each ten).</summary>
    public int Condition { get; internal set; }
    public ConditionChange ConditionMod { get; internal set; }

    public AppealMove? CurrMove { get; internal set; }
    public AppealMove? PrevMove { get; internal set; }

    /// <summary>The points of this turn's jams, and the share of them a contestant is safe from.</summary>
    public int Jam { get; internal set; }
    public int JamSafetyCount { get; internal set; }
    public int JamReduction { get; internal set; }
    public bool Resistant { get; internal set; }
    public bool Immune { get; internal set; }
    public bool MoreEasilyStartled { get; internal set; }
    public bool UsedRepeatableMove { get; internal set; }

    public bool Nervous { get; internal set; }
    public bool Exploded { get; internal set; }
    public bool NoMoreTurns { get; internal set; }
    public bool TurnSkipped { get; internal set; }
    public int NumTurnsSkipped { get; internal set; }

    public bool HasJudgesAttention { get; internal set; }
    public bool JudgesAttentionWasRemoved { get; internal set; }
    public bool CompletedCombo { get; internal set; }
    public bool CompletedComboFlag { get; internal set; }
    public bool UsedComboMove { get; internal set; }
    public int ComboAppealBonus { get; internal set; }

    public bool RepeatedMove { get; internal set; }
    public bool RepeatedPrevMove { get; internal set; }
    public int MoveRepeatCount { get; internal set; }
    public int RepeatJam { get; internal set; }

    /// <summary>The place in line the next round was asked for (255 for none), and which way the move bent it (1 earlier, 2 later, 3 scrambled).</summary>
    public int NextTurnOrder { get; internal set; } = AppealRound.NoOrder;
    public int TurnOrderModAction { get; internal set; }

    public bool AppealTripleCondition { get; internal set; }
    public bool OverrideCategoryExcitement { get; internal set; }

    /// <summary>What was said of the contestant in the turn that is playing: its own line and a second.</summary>
    public AppealLine? Line { get; internal set; }
    public AppealLine? Line2 { get; internal set; }

    /// <summary>The move's quality, as Emerald keeps it for the status (<c>moveCategory</c>).</summary>
    public ContestStat MoveCategory { get; internal set; }

    /// <summary>Whether the turn is lost: skipped for a move's price, or spent for good.</summary>
    public bool TurnDisabled => NumTurnsSkipped != 0 || NoMoreTurns;
}

/// <summary>
/// The lines an appeal's effects say about a contestant (Emerald's <c>CONTEST_STRING_*</c>, by the names the
/// decompilation gives them): each is <c>appeal.line.&lt;Name&gt;</c> in the interface table, with the Pokémon's
/// name, the move and the quality's fear to fill in.
/// </summary>
public enum AppealLine
{
    MoreConscious, NoAppeal, SettleDown, ObliviousToOthers, LessAware, StoppedCaring, AttemptStartle, DazzleAttempt,
    JudgeLookAway, UnnerveAttempt, Nervous, UnnerveWaiting, TauntWell, RegainedForm, JamWell, HustleStandout,
    WorkHardUnnoticed, WorkBefore, AppealNotWell, WorkPreceding, AppealNotWell2, NotShownWell, SlightlyWell,
    PrettyWell, Excellently, AppealNotVeryWell, SlightlyWell2, PrettyWell2, VeryWell, Excellently2, SameTypeGood,
    DiffTypeGood, StoodOutAsMuch, NotAsWell, ConditionRose, HotStatus, MoveUpLine, MoveBackLine, ScrambleOrder,
    AvertGaze, AvoidSeeing, NotFazed, LittleDistracted, LookedDown, TurnedBack, UtterCry, LeaptUp, TrippedOver,
    MessedUp, Ignored, NoConditionImprove, BadConditionWeakAppeal, Unaffected, AttractedAttention
}

/// <summary>What a turn of the appeals shows, in the order the stage plays it.</summary>
public enum AppealEventKind
{
    /// <summary>The Pokémon was watching the others (its turn was lost).</summary>
    Watching,
    /// <summary>It was too nervous to move.</summary>
    TooNervous,
    /// <summary>It appealed with the move: <see cref="AppealEvent.Amount"/> is the points the appeal is worth now.</summary>
    Appealed,
    /// <summary>A line about <see cref="AppealEvent.Who"/> from an effect.</summary>
    Line,
    /// <summary>The next round's order was bent (1 up, 2 back, 3 scrambled).</summary>
    TurnOrder,
    /// <summary>The mover's condition rose (<see cref="AppealEvent.Amount"/> is the stars' tens now).</summary>
    ConditionUp,
    /// <summary><see cref="AppealEvent.Who"/> lost its condition.</summary>
    ConditionDown,
    /// <summary><see cref="AppealEvent.Who"/> was startled: <see cref="AppealEvent.Amount"/> points lost.</summary>
    Jammed,
    /// <summary><see cref="AppealEvent.Who"/> can't appeal the next turn.</summary>
    SkipsNext,
    /// <summary>The judge looked at the Pokémon expectantly: a combo was begun.</summary>
    ComboStarted,
    /// <summary>The combo was finished: <see cref="AppealEvent.Amount"/> points added.</summary>
    ComboDone,
    /// <summary>The same appeal twice: <see cref="AppealEvent.Amount"/> points lost.</summary>
    Repeated,
    /// <summary>The crowd got going: <see cref="AppealEvent.Amount"/> is the points of the bonus (10, or 60 when the meter overflowed), <see cref="AppealEvent.Other"/> the meter now.</summary>
    CrowdUp,
    /// <summary>The crowd cooled: <see cref="AppealEvent.Other"/> is the meter now.</summary>
    CrowdDown,
    /// <summary>The crowd goes on watching the one who froze it, and the move is ignored.</summary>
    CrowdWatches
}

/// <summary>One thing that happened in a turn.</summary>
public sealed record AppealEvent(AppealEventKind Kind, int Who, int Amount = 0, int Other = 0, AppealLine? Line = null, string Move = "");

/// <summary>
/// The appeals of a Hoenn contest (plan 21 · H11; the original's <c>src/contest.c</c> and <c>src/contest_effect.c</c>,
/// Emerald's): four Pokémon, five rounds of four turns. Before the first round the contestants are ranked by the
/// condition of the contest's quality (<see cref="Appealer.Round1Points"/>); in each round everyone chooses a move
/// (the opponents by <see cref="AppealRound"/>'s thinking, port of Emerald's contest AI scripts) and then appeals in
/// the turn order, a move's effect adding hearts, jamming those who went before, bending the order, stirring the
/// crowd or throwing the judge's eye about. The score is the hearts won twice over added to the condition's points.
/// Every roll is on the generator it is given, so a test that hands one in knows what a round comes to. No drawing, no
/// input, no clock.
/// </summary>
public sealed partial class AppealRound
{
    public const int Contestants = 4, Appeals = 5, ApplauseSize = 5, MaxCondition = 30, NoOrder = 255;

    public ContestStat Category { get; }
    public AppealRank Rank { get; }
    public IReadOnlyList<Appealer> Appealers { get; }
    public IReadOnlyList<AppealStatus> Status => status;

    private readonly AppealStatus[] status = new AppealStatus[Contestants];
    private readonly Random rng;

    /// <summary>The round of appeals (0 to 4) and the turn in it (0 to 3); five rounds done is <see cref="IsOver"/>.</summary>
    public int AppealNumber { get; private set; }
    public int TurnNumber { get; private set; }
    public bool IsOver => AppealNumber >= Appeals;

    /// <summary>The crowd's excitement, 0 to 4 (5 resets it), and whether a move is holding it still and who.</summary>
    public int ApplauseLevel { get; private set; }
    public bool CrowdFrozen { get; private set; }
    public int Freezer { get; private set; } = -1;

    /// <summary>The turn each contestant has this round, 0 for the first.</summary>
    public IReadOnlyList<int> TurnOrder => turnOrder;
    private readonly int[] turnOrder = new int[Contestants];

    /// <summary>The score of the whole contest once the rounds are over: the appeals' points twice over and the condition's.</summary>
    public int[] Round2Points { get; } = new int[Contestants];
    public int[] TotalPoints { get; } = new int[Contestants];

    /// <summary>0 for the winner; set when the last round is over.</summary>
    public int[] FinalStandings { get; } = new int[Contestants];

    // The state of the appeal that is being worked out (the original's eContestAppealResults)
    private int mover;
    private int jam, jam2;
    private readonly List<int> jamQueue = new();
    private int moveExcitement, excitementAppealBonus;
    private bool movesChosen;

    /// <summary>The move history: for each round of appeals, what each contestant used.</summary>
    public IReadOnlyList<IReadOnlyList<AppealMove?>> History => history;
    private readonly List<AppealMove?[]> history = new();

    public AppealRound(IReadOnlyList<Appealer> appealers, ContestStat category, AppealRank rank, Random chance)
    {
        if (appealers.Count != Contestants) throw new ArgumentException("A contest has four on the stage.", nameof(appealers));
        Appealers = appealers;
        Category = category;
        Rank = rank;
        rng = chance;
        for (int i = 0; i < Contestants; i++) status[i] = new AppealStatus();
        foreach (var a in appealers) a.Round1Points = Round1Points(category, a);

        // InitContestResources: the first order is the condition's (SortContestants(FALSE)), then ApplyNextTurnOrder
        SortByCondition();
        ApplyNextTurnOrder();
    }

    // ------------------------------------------------------------------ the condition's points and who goes first

    /// <summary>
    /// A Pokémon's first-round points for a contest (<c>CalculateContestantRound1Points</c>): the quality plus half of
    /// its two neighbours' and the sheen together (Cool's neighbours are Tough and Beauty, Beauty's Cool and Cute, and
    /// so on round).
    /// </summary>
    public static int Round1Points(ContestStat category, Appealer a)
    {
        int Of(int quality) => a.Stats[((quality % 5) + 5) % 5];
        int main = (int)category;
        // Emerald's order: sub1 is the quality before (Cool: Tough), sub2 the one after (Cool: Beauty), by its table
        int sub1 = Of(main - 1), sub2 = Of(main + 1);
        return Of(main) + (sub1 + sub2 + a.Sheen) / 2;
    }

    private int Next16() => rng.Next(65536);

    private int[] DrawOrdering()
    {
        // A unique random number for each contestant (the loop of SortContestants)
        var ordering = new int[Contestants];
        for (int i = 0; i < Contestants; i++)
        {
            ordering[i] = Next16();
            for (int j = 0; j < i; j++)
                if (ordering[i] == ordering[j]) { i--; break; }
        }
        return ordering;
    }

    private void SortByCondition()
    {
        var randomOrdering = DrawOrdering();
        // The contestants in order: the best condition first, a tie by the larger draw (insertion sort as the original does)
        var list = new List<int>();
        for (int i = 0; i < Contestants; i++)
        {
            int at = list.Count;
            for (int v = 0; v < list.Count; v++)
            {
                int other = list[v];
                if (Appealers[other].Round1Points < Appealers[i].Round1Points
                    || (Appealers[other].Round1Points == Appealers[i].Round1Points && randomOrdering[other] < randomOrdering[i]))
                {
                    at = v;
                    break;
                }
            }
            list.Insert(at, i);
        }
        // Inverted: the contestant is the index, the turn the data
        for (int turn = 0; turn < Contestants; turn++) turnOrder[list[turn]] = turn;
    }

    private void SortByRanking()
    {
        var randomOrdering = DrawOrdering();
        var slots = Enumerable.Repeat(NoOrder, Contestants).ToArray();
        for (int i = 0; i < Contestants; i++)
        {
            int j = status[i].Ranking;
            while (true)
            {
                if (slots[j] == NoOrder)
                {
                    slots[j] = i;
                    turnOrder[i] = j;
                    break;
                }
                j++;
            }
        }
        // Tied rankings are shuffled by the draws, with the original's selection sort
        for (int i = 0; i < Contestants - 1; i++)
            for (int v = Contestants - 1; v > i; v--)
                if (status[v - 1].Ranking == status[v].Ranking && turnOrder[v - 1] < turnOrder[v] && randomOrdering[v - 1] < randomOrdering[v])
                    (turnOrder[v], turnOrder[v - 1]) = (turnOrder[v - 1], turnOrder[v]);
    }

    /// <summary>
    /// The order for the next round (<c>ApplyNextTurnOrder</c>): the turns a move asked for are given first, and the rest
    /// go to the contestants in the order they had.
    /// </summary>
    private void ApplyNextTurnOrder()
    {
        int nextContestant = 0;
        var newOrder = (int[])turnOrder.Clone();
        var ordered = new bool[Contestants];
        for (int i = 0; i < Contestants; i++)
        {
            int j;
            for (j = 0; j < Contestants; j++)
            {
                if (status[j].NextTurnOrder == i)
                {
                    newOrder[j] = i;
                    ordered[j] = true;
                    break;
                }
            }
            if (j == Contestants)
            {
                for (j = 0; j < Contestants; j++)
                {
                    if (!ordered[j] && status[j].NextTurnOrder == NoOrder)
                    {
                        nextContestant = j;
                        j++;
                        break;
                    }
                }
                for (; j < Contestants; j++)
                    if (!ordered[j] && status[j].NextTurnOrder == NoOrder && turnOrder[nextContestant] > turnOrder[j])
                        nextContestant = j;
                newOrder[nextContestant] = i;
                ordered[nextContestant] = true;
            }
        }
        for (int i = 0; i < Contestants; i++)
        {
            turnOrder[i] = newOrder[i];
            status[i].NextTurnOrder = NoOrder;
        }
    }

    /// <summary>The contestant whose turn it is to appeal at a place in line.</summary>
    public int ContestantAtTurn(int turn)
    {
        for (int i = 0; i < Contestants; i++)
            if (turnOrder[i] == turn) return i;
        return 0;
    }

    /// <summary>Whether the contestant can take a turn this round (not skipped, not spent).</summary>
    public bool CanAppeal(int contestant) => !status[contestant].TurnDisabled;

    // ------------------------------------------------------------------ choosing

    /// <summary>
    /// Everyone chooses a move for the round (<c>GetAllChosenMoves</c>): the player's by the index of its moves, the
    /// opponents' by their thinking. A contestant who can't take the turn chooses none.
    /// </summary>
    public void ChooseMoves(int playerMove)
    {
        if (IsOver) throw new InvalidOperationException("The appeals are over.");
        for (int i = 0; i < Contestants; i++)
        {
            var a = Appealers[i];
            if (status[i].TurnDisabled) { status[i].CurrMove = null; continue; }
            if (a.IsPlayer)
            {
                if (playerMove < 0 || playerMove >= a.Moves.Count) throw new ArgumentOutOfRangeException(nameof(playerMove));
                status[i].CurrMove = a.Moves[playerMove];
            }
            else
            {
                status[i].CurrMove = a.Moves[ChooseFor(i)];
            }
        }
        movesChosen = true;
        TurnNumber = 0;
    }

    /// <summary>
    /// Everyone's move for the round is given (tests, a recorded round): the index of each contestant's move, in the
    /// order of the stage, which a contestant who can't take the turn ignores.
    /// </summary>
    public void ChooseMoves(IReadOnlyList<int> picks)
    {
        if (IsOver) throw new InvalidOperationException("The appeals are over.");
        if (picks.Count != Contestants) throw new ArgumentException("A move for each of the four.", nameof(picks));
        for (int i = 0; i < Contestants; i++)
        {
            var a = Appealers[i];
            if (status[i].TurnDisabled) { status[i].CurrMove = null; continue; }
            if (picks[i] < 0 || picks[i] >= a.Moves.Count) throw new ArgumentOutOfRangeException(nameof(picks));
            status[i].CurrMove = a.Moves[picks[i]];
        }
        movesChosen = true;
        TurnNumber = 0;
    }

    /// <summary>The index of the player's contestant on the stage.</summary>
    public int PlayerIndex => Appealers.ToList().FindIndex(a => a.IsPlayer);

    // ------------------------------------------------------------------ a turn

    private AppealStatus Mover => status[mover];

    /// <summary>
    /// Plays the next turn of the round (the original's <c>Task_DoAppeals</c> from <c>CalculateAppealMoveImpact</c> to the
    /// crowd) and says what happened, in the order the stage shows it. After the fourth turn the round is ranked and
    /// the next one made ready.
    /// </summary>
    public IReadOnlyList<AppealEvent> PlayTurn()
    {
        if (IsOver) throw new InvalidOperationException("The appeals are over.");
        if (!movesChosen) throw new InvalidOperationException("The moves of the round have not been chosen.");
        var events = new List<AppealEvent>();
        int c = ContestantAtTurn(TurnNumber);
        mover = c;
        CalculateAppealMoveImpact(c);
        var s = status[c];
        string moveName = s.CurrMove?.Name ?? "";

        if (s.TurnDisabled)
        {
            events.Add(new AppealEvent(AppealEventKind.Watching, c));
        }
        else if (s.Nervous)
        {
            s.HasJudgesAttention = false;
            events.Add(new AppealEvent(AppealEventKind.TooNervous, c, Move: moveName));
        }
        else
        {
            events.Add(new AppealEvent(AppealEventKind.Appealed, c, s.Appeal, Move: moveName));
            // The move's own lines: the first, then the second only if nobody else has a line to say
            if (s.Line is { } line) { events.Add(new AppealEvent(AppealEventKind.Line, c, Line: line, Move: moveName)); s.Line = null; }
            if (s.Line2 is { } line2 && Enumerable.Range(0, Contestants).All(i => i == c || status[i].Line == null))
            {
                events.Add(new AppealEvent(AppealEventKind.Line, c, Line: line2, Move: moveName));
                s.Line2 = null;
            }
            if (s.TurnOrderModAction != 0) events.Add(new AppealEvent(AppealEventKind.TurnOrder, c, s.TurnOrderModAction));
            if (s.ConditionMod == ConditionChange.Gain) events.Add(new AppealEvent(AppealEventKind.ConditionUp, c, s.Condition));
            // Those the move touched, in the order of their turns
            for (int turn = 0; turn < Contestants; turn++)
            {
                int j = ContestantAtTurn(turn);
                if (j == c || status[j].Line is not { } theirs) continue;
                events.Add(new AppealEvent(AppealEventKind.Line, j, Line: theirs, Move: moveName));
                status[j].Line = null;
                if (status[j].Jam != 0) events.Add(new AppealEvent(AppealEventKind.Jammed, j, status[j].Jam));
                if (status[j].ConditionMod == ConditionChange.Lose) events.Add(new AppealEvent(AppealEventKind.ConditionDown, j, 0));
            }
            // A jam that came with no line of its own
            for (int j = 0; j < Contestants; j++)
                if (j != c && status[j].Jam != 0 && !events.Any(e => e.Kind == AppealEventKind.Jammed && e.Who == j))
                    events.Add(new AppealEvent(AppealEventKind.Jammed, j, status[j].Jam));
            if (s.NumTurnsSkipped != 0 || s.TurnSkipped) events.Add(new AppealEvent(AppealEventKind.SkipsNext, c));

            // The combo
            if (s.UsedComboMove)
            {
                if (s.CompletedCombo)
                {
                    // Finished: the bonus is added once the judge's eye is gone
                    s.Appeal += s.ComboAppealBonus;
                    events.Add(new AppealEvent(AppealEventKind.ComboDone, c, s.ComboAppealBonus));
                }
                else
                {
                    events.Add(new AppealEvent(AppealEventKind.ComboStarted, c));
                }
            }
            // The same move twice
            if (s.RepeatedMove)
            {
                s.Appeal -= s.RepeatJam;
                events.Add(new AppealEvent(AppealEventKind.Repeated, c, s.RepeatJam));
            }
            // The crowd
            if (CrowdFrozen && c != Freezer)
            {
                events.Add(new AppealEvent(AppealEventKind.CrowdWatches, c, Other: Freezer, Move: moveName));
            }
            else
            {
                int excite = moveExcitement;
                if (s.OverrideCategoryExcitement) excite = 1;
                if (excite > 0 && s.RepeatedMove) excite = 0;
                ApplauseLevel += excite;
                if (ApplauseLevel < 0) ApplauseLevel = 0;
                if (excite < 0)
                {
                    events.Add(new AppealEvent(AppealEventKind.CrowdDown, c, Other: ApplauseLevel));
                }
                else if (excite > 0)
                {
                    s.Appeal += excitementAppealBonus;
                    events.Add(new AppealEvent(AppealEventKind.CrowdUp, c, excitementAppealBonus, ApplauseLevel));
                }
                // A meter past its last light starts again
                if (ApplauseLevel > ApplauseSize - 1) ApplauseLevel = 0;
            }
        }

        TurnNumber++;
        if (TurnNumber == Contestants) FinishRound();
        return events;
    }

    /// <summary>
    /// After the fourth turn (<c>RankContestants</c>, <c>SetContestantStatusesForNextRound</c>): the appeals' points are
    /// added, the contestants ranked and the order of the next round made, and everything that lasted a round is cleared.
    /// </summary>
    private void FinishRound()
    {
        for (int i = 0; i < Contestants; i++) status[i].PointTotal += status[i].Appeal;
        var totals = status.Select(s => s.PointTotal).OrderByDescending(t => t).ToArray();
        for (int i = 0; i < Contestants; i++)
            status[i].Ranking = Array.IndexOf(totals, status[i].PointTotal);
        SortByRanking();
        ApplyNextTurnOrder();

        // SetContestantStatusesForNextRound
        var used = new AppealMove?[Contestants];
        for (int i = 0; i < Contestants; i++)
        {
            var s = status[i];
            s.Appeal = 0;
            s.BaseAppeal = 0;
            s.JamSafetyCount = 0;
            if (s.NumTurnsSkipped > 0) s.NumTurnsSkipped--;
            s.Jam = 0;
            s.Resistant = false;
            s.JamReduction = 0;
            s.Immune = false;
            s.MoreEasilyStartled = false;
            s.UsedRepeatableMove = false;
            s.Nervous = false;
            s.Line = null;
            s.Line2 = null;
            s.ConditionMod = ConditionChange.None;
            s.RepeatedPrevMove = s.RepeatedMove;
            s.RepeatedMove = false;
            s.TurnOrderModAction = 0;
            s.AppealTripleCondition = false;
            if (s.TurnSkipped)
            {
                s.NumTurnsSkipped = 1;
                s.TurnSkipped = false;
            }
            if (s.Exploded)
            {
                s.NoMoreTurns = true;
                s.Exploded = false;
            }
            s.OverrideCategoryExcitement = false;
        }
        for (int i = 0; i < Contestants; i++)
        {
            used[i] = status[i].CurrMove;
            status[i].PrevMove = status[i].CurrMove;
            status[i].CurrMove = null;
        }
        history.Add(used);
        CrowdFrozen = false;
        Freezer = -1;
        movesChosen = false;
        AppealNumber++;
        TurnNumber = 0;
        if (AppealNumber >= Appeals) Conclude();
    }

    /// <summary>The final scores and places (<c>CalculateFinalScores</c>, <c>DetermineFinalStandings</c>).</summary>
    private void Conclude()
    {
        var randomOrdering = DrawOrdering();
        for (int i = 0; i < Contestants; i++)
        {
            Round2Points[i] = status[i].PointTotal * 2;
            TotalPoints[i] = Appealers[i].Round1Points + Round2Points[i];
        }
        // Total points, then the condition's, then the larger draw
        var order = Enumerable.Range(0, Contestants).ToList();
        order.Sort((a, b) =>
        {
            if (TotalPoints[a] != TotalPoints[b]) return TotalPoints[b].CompareTo(TotalPoints[a]);
            if (Appealers[a].Round1Points != Appealers[b].Round1Points) return Appealers[b].Round1Points.CompareTo(Appealers[a].Round1Points);
            return randomOrdering[b].CompareTo(randomOrdering[a]);
        });
        for (int place = 0; place < Contestants; place++) FinalStandings[order[place]] = place;
    }

    /// <summary>The points a contestant has now: what it has won over the rounds and the appeal under way.</summary>
    public int PointsNow(int contestant) => status[contestant].PointTotal + status[contestant].Appeal;

    // ------------------------------------------------------------------ CalculateAppealMoveImpact

    private void CalculateAppealMoveImpact(int c)
    {
        var s = status[c];
        s.Appeal = 0;
        s.BaseAppeal = 0;
        if (s.TurnDisabled) return;

        var move = s.CurrMove;
        var contest = move?.Contest ?? NoMove;
        var effect = HoennContestDatabase.Effect(contest.Effect);

        s.MoveCategory = contest.Category;
        if (move != null && s.PrevMove != null && move.Name == s.PrevMove.Name)
        {
            s.RepeatedMove = true;
            s.MoveRepeatCount++;
        }
        else
        {
            s.MoveRepeatCount = 0;
        }
        s.BaseAppeal = effect.Appeal;
        s.Appeal = s.BaseAppeal;
        jam = effect.Jam;
        jam2 = jam;
        for (int i = 0; i < Contestants; i++) status[i].Jam = 0;

        if (s.HasJudgesAttention && !(s.PrevMove != null && move != null && HoennContestDatabase.AreCombo(s.PrevMove.Contest, contest)))
            s.HasJudgesAttention = false;

        RunEffect(contest.Effect);

        if (s.ConditionMod == ConditionChange.Gain) s.Appeal += s.Condition - 10;
        else if (s.AppealTripleCondition) s.Appeal += s.Condition * 3;
        else s.Appeal += s.Condition;

        s.CompletedCombo = false;
        s.UsedComboMove = false;
        if (CanCombo(c))
        {
            bool completed = s.PrevMove != null && move != null && HoennContestDatabase.AreCombo(s.PrevMove.Contest, contest);
            if (completed && s.HasJudgesAttention)
            {
                s.CompletedCombo = true;
                s.UsedComboMove = true;
                s.HasJudgesAttention = false;
                s.ComboAppealBonus = s.BaseAppeal;
                s.CompletedComboFlag = true;
            }
            else if (contest.ComboStarter != 0)
            {
                s.HasJudgesAttention = true;
                s.UsedComboMove = true;
            }
            else
            {
                s.HasJudgesAttention = false;
            }
        }
        if (s.RepeatedMove) s.RepeatJam = (s.MoveRepeatCount + 1) * 10;

        if (s.Nervous)
        {
            s.HasJudgesAttention = false;
            s.Appeal = 0;
            s.BaseAppeal = 0;
        }
        moveExcitement = move == null ? AppealRules.Excitement(Category, ContestStat.Cool) : AppealRules.Excitement(Category, contest.Category);
        if (s.OverrideCategoryExcitement) moveExcitement = 1;
        excitementAppealBonus = moveExcitement > 0 ? (ApplauseLevel + moveExcitement > 4 ? 60 : 10) : 0;
    }

    private static readonly HoennContestMove NoMove = new() { Effect = 0, Category = ContestStat.Cool, ComboStarter = 0, Combos = new() };

    /// <summary>A contestant can chain a combo unless it repeated its move or is nervous (<c>IsContestantAllowedToCombo</c>).</summary>
    private bool CanCombo(int c) => !status[c].RepeatedMove && !status[c].Nervous;

    // ------------------------------------------------------------------ the effects (contest_effect.c)

    private void Say(int who, AppealLine line) => status[who].Line = line;
    private void Say2(int who, AppealLine line) => status[who].Line2 = line;

    private void RunEffect(int effect)
    {
        var s = Mover;
        switch (effect)
        {
            case 0: break; // highly appealing
            case 1: s.MoreEasilyStartled = true; Say(mover, AppealLine.MoreConscious); break;
            case 2: s.Exploded = true; Say(mover, AppealLine.NoAppeal); break;
            case 3: s.UsedRepeatableMove = true; s.RepeatedMove = false; s.MoveRepeatCount = 0; break;
            case 4: s.JamSafetyCount = 1; Say(mover, AppealLine.SettleDown); break;
            case 5: s.Immune = true; Say(mover, AppealLine.ObliviousToOthers); break;
            case 6: s.JamReduction = 20; Say(mover, AppealLine.LessAware); break;
            case 7: s.Resistant = true; Say(mover, AppealLine.StoppedCaring); break;
            case 8: case 10: case 12: StartleFrontMon(); break;
            case 9: case 11: case 13: StartlePrevMons(); break;
            case 14: StartlePrevMon2(); break;
            case 15: StartlePrevMons2(); break;
            case 16: ShiftJudgeAttention(); break;
            case 17: StartleMonWithJudgesAttention(); break;
            case 18: s.TurnSkipped = true; StartlePrevMons(); Say(mover, AppealLine.AttemptStartle); break;
            case 19: JamByMoveCategory(s.CurrMove!.Contest.Category); Say(mover, AppealLine.AttemptStartle); break;
            case 20: JamByMoveCategory(ContestStat.Cool); Say(mover, AppealLine.AttemptStartle); break;
            case 21: JamByMoveCategory(ContestStat.Beauty); Say(mover, AppealLine.AttemptStartle); break;
            case 22: JamByMoveCategory(ContestStat.Cute); Say(mover, AppealLine.AttemptStartle); break;
            case 23: JamByMoveCategory(ContestStat.Smart); Say(mover, AppealLine.AttemptStartle); break;
            case 24: JamByMoveCategory(ContestStat.Tough); Say(mover, AppealLine.AttemptStartle); break;
            case 25: MakeFollowingMonNervous(); break;
            case 26: MakeFollowingMonsNervous(); break;
            case 27: WorsenConditionOfPrevMons(); break;
            case 28: BadlyStartlesMonsInGoodCondition(); break;
            case 29: BetterIfFirst(); break;
            case 30: BetterIfLast(); break;
            case 31: AppealAsGoodAsPrevOnes(); break;
            case 32: AppealAsGoodAsPrevOne(); break;
            case 33: BetterWhenLater(); break;
            case 34: QualityDependsOnTiming(); break;
            case 35: BetterIfSameType(); break;
            case 36: BetterIfDiffType(); break;
            case 37: AffectedByPrevAppeal(); break;
            case 38: ImproveCondition(); break;
            case 39: s.AppealTripleCondition = true; Say(mover, s.Condition != 0 ? AppealLine.HotStatus : AppealLine.BadConditionWeakAppeal); break;
            case 40: NextAppealEarlier(); break;
            case 41: NextAppealLater(); break;
            case 42: break; // dummied out in the original
            case 43: ScrambleNextTurnOrder(); break;
            case 44:
                if (s.CurrMove!.Contest.Category != Category) s.OverrideCategoryExcitement = true;
                break;
            case 45: BadlyStartleMonsWithGoodAppeals(); break;
            case 46: BetterWhenAudienceExcited(); break;
            case 47:
                if (!CrowdFrozen)
                {
                    CrowdFrozen = true;
                    Freezer = mover;
                    Say(mover, AppealLine.AttractedAttention);
                }
                break;
        }
    }

    private void StartleFrontMon()
    {
        int idx = 0;
        if (turnOrder[mover] != 0)
        {
            int i;
            for (i = 0; i < Contestants; i++)
                if (turnOrder[mover] - 1 == turnOrder[i]) break;
            jamQueue.Clear();
            jamQueue.Add(i);
            idx = WasAtLeastOneOpponentJammed() ? 1 : 0;
        }
        if (idx == 0) Say2(mover, AppealLine.MessedUp);
        Say(mover, AppealLine.AttemptStartle);
    }

    private void StartlePrevMons()
    {
        int idx = 0;
        if (turnOrder[mover] != 0)
        {
            jamQueue.Clear();
            for (int i = 0; i < Contestants; i++)
                if (turnOrder[mover] > turnOrder[i]) jamQueue.Add(i);
            idx = WasAtLeastOneOpponentJammed() ? 1 : 0;
        }
        if (idx == 0) Say2(mover, AppealLine.MessedUp);
        Say(mover, AppealLine.AttemptStartle);
    }

    private void StartlePrevMon2()
    {
        int r = rng.Next(10);
        jam = r < 2 ? 20 : r < 8 ? 40 : 60;
        StartleFrontMon();
    }

    private void StartlePrevMons2()
    {
        int numStartled = 0;
        if (turnOrder[mover] != 0)
        {
            for (int i = 0; i < Contestants; i++)
            {
                if (turnOrder[mover] > turnOrder[i])
                {
                    jamQueue.Clear();
                    jamQueue.Add(i);
                    int r = rng.Next(10);
                    jam = r == 0 ? 0 : r <= 2 ? 10 : r <= 4 ? 20 : r <= 6 ? 30 : r <= 8 ? 40 : 60;
                    if (WasAtLeastOneOpponentJammed()) numStartled++;
                }
            }
        }
        Say(mover, AppealLine.AttemptStartle);
        if (numStartled == 0) Say2(mover, AppealLine.MessedUp);
    }

    private void ShiftJudgeAttention()
    {
        bool hitAny = false;
        if (turnOrder[mover] != 0)
        {
            for (int i = 0; i < Contestants; i++)
            {
                if (turnOrder[mover] > turnOrder[i] && status[i].HasJudgesAttention && CanUnnerve(i))
                {
                    status[i].HasJudgesAttention = false;
                    status[i].JudgesAttentionWasRemoved = true;
                    Say(i, AppealLine.JudgeLookAway);
                    hitAny = true;
                }
            }
        }
        Say(mover, AppealLine.DazzleAttempt);
        if (!hitAny) Say2(mover, AppealLine.MessedUp);
    }

    private void StartleMonWithJudgesAttention()
    {
        int numStartled = 0;
        if (turnOrder[mover] != 0)
        {
            for (int i = 0; i < Contestants; i++)
            {
                if (turnOrder[mover] > turnOrder[i])
                {
                    jam = status[i].HasJudgesAttention ? 50 : 10;
                    jamQueue.Clear();
                    jamQueue.Add(i);
                    if (WasAtLeastOneOpponentJammed()) numStartled++;
                }
            }
        }
        Say(mover, AppealLine.AttemptStartle);
        if (numStartled == 0) Say2(mover, AppealLine.MessedUp);
    }

    private void JamByMoveCategory(ContestStat category)
    {
        int numJammed = 0;
        for (int i = 0; i < Contestants; i++)
        {
            if (turnOrder[mover] > turnOrder[i])
            {
                // A contestant that didn't appeal (nervous, or its turn lost) has no move: it counts as Emerald's move 0, a Cool one
                var theirs = status[i].CurrMove?.Contest.Category ?? ContestStat.Cool;
                jam = category == theirs ? 40 : 10;
                jamQueue.Clear();
                jamQueue.Add(i);
                if (WasAtLeastOneOpponentJammed()) numJammed++;
            }
        }
        if (numJammed == 0) Say2(mover, AppealLine.MessedUp);
    }

    private void MakeFollowingMonNervous()
    {
        bool hitAny = false;
        if (turnOrder[mover] != 3)
        {
            for (int i = 0; i < Contestants; i++)
            {
                if (turnOrder[mover] + 1 == turnOrder[i])
                {
                    if (CanUnnerve(i))
                    {
                        MakeNervous(i);
                        Say(i, AppealLine.Nervous);
                        hitAny = true;
                    }
                    else
                    {
                        Say(i, AppealLine.Unaffected);
                        hitAny = true;
                    }
                }
            }
        }
        Say(mover, AppealLine.UnnerveAttempt);
        if (!hitAny) Say2(mover, AppealLine.MessedUp);
    }

    private void MakeFollowingMonsNervous()
    {
        int numUnnerved = 0;
        var ids = new List<int>();
        for (int i = 0; i < Contestants; i++)
            if (turnOrder[mover] < turnOrder[i] && !status[i].Nervous && !status[i].TurnDisabled) ids.Add(i);

        var odds = new int[Contestants];
        if (ids.Count == 1) odds[0] = 60;
        else if (ids.Count == 2) { odds[0] = 30; odds[1] = 30; }
        else if (ids.Count == 3) { odds[0] = 20; odds[1] = 20; odds[2] = 20; }

        var oddsMod = new int[Contestants];
        for (int i = 0; i < Contestants; i++)
        {
            oddsMod[i] = status[i].HasJudgesAttention && CanCombo(i) && status[i].PrevMove != null && status[i].PrevMove!.Contest.ComboStarter != 0 ? 10 : 0;
            oddsMod[i] -= status[i].Condition / 10 * 10;
        }
        if (odds[0] != 0)
        {
            for (int k = 0; k < ids.Count; k++)
            {
                int who = ids[k];
                bool unaffected = false;
                if (rng.Next(100) < odds[k] + oddsMod[who])
                {
                    if (CanUnnerve(who))
                    {
                        MakeNervous(who);
                        Say(who, AppealLine.Nervous);
                        numUnnerved++;
                    }
                    else
                    {
                        unaffected = true;
                    }
                }
                else
                {
                    unaffected = true;
                }
                if (unaffected)
                {
                    Say(who, AppealLine.Unaffected);
                    numUnnerved++;
                }
            }
        }
        Say(mover, AppealLine.UnnerveWaiting);
        if (numUnnerved == 0) Say2(mover, AppealLine.MessedUp);
    }

    private void WorsenConditionOfPrevMons()
    {
        int numHit = 0;
        for (int i = 0; i < Contestants; i++)
        {
            if (turnOrder[mover] > turnOrder[i] && status[i].Condition > 0 && CanUnnerve(i))
            {
                status[i].Condition = 0;
                status[i].ConditionMod = ConditionChange.Lose;
                Say(i, AppealLine.RegainedForm);
                numHit++;
            }
        }
        Say(mover, AppealLine.TauntWell);
        if (numHit == 0) Say2(mover, AppealLine.Ignored);
    }

    private void BadlyStartlesMonsInGoodCondition()
    {
        int numHit = 0;
        for (int i = 0; i < Contestants; i++)
        {
            if (turnOrder[mover] > turnOrder[i])
            {
                jam = status[i].Condition > 0 ? 40 : 10;
                jamQueue.Clear();
                jamQueue.Add(i);
                if (WasAtLeastOneOpponentJammed()) numHit++;
            }
        }
        Say(mover, AppealLine.JamWell);
        if (numHit == 0) Say2(mover, AppealLine.Ignored);
    }

    private int EffectAppealOfMove() => HoennContestDatabase.Effect(Mover.CurrMove!.Contest.Effect).Appeal;

    private void BetterIfFirst()
    {
        if (turnOrder[mover] == 0)
        {
            Mover.Appeal += 2 * EffectAppealOfMove();
            Say(mover, AppealLine.HustleStandout);
        }
    }

    private void BetterIfLast()
    {
        if (turnOrder[mover] == 3)
        {
            Mover.Appeal += 2 * EffectAppealOfMove();
            Say(mover, AppealLine.WorkHardUnnoticed);
        }
    }

    private void AppealAsGoodAsPrevOnes()
    {
        int sum = 0;
        for (int i = 0; i < Contestants; i++)
            if (turnOrder[mover] > turnOrder[i]) sum += status[i].Appeal;
        if (sum < 0) sum = 0;
        if (turnOrder[mover] == 0 || sum == 0)
        {
            Say(mover, AppealLine.AppealNotWell);
        }
        else
        {
            Mover.Appeal += sum / 2;
            Say(mover, AppealLine.WorkBefore);
        }
        Mover.Appeal = RoundTowardsZero(Mover.Appeal);
    }

    private void AppealAsGoodAsPrevOne()
    {
        int appeal = 0;
        if (turnOrder[mover] != 0)
            for (int i = 0; i < Contestants; i++)
                if (turnOrder[mover] - 1 == turnOrder[i]) appeal = status[i].Appeal;
        if (turnOrder[mover] == 0 || appeal <= 0)
        {
            Say(mover, AppealLine.AppealNotWell2);
        }
        else
        {
            Mover.Appeal += appeal;
            Say(mover, AppealLine.WorkPreceding);
        }
    }

    private void BetterWhenLater()
    {
        int turn = turnOrder[mover];
        Mover.Appeal = turn == 0 ? 10 : 20 * turn;
        Say(mover, turn switch { 0 => AppealLine.NotShownWell, 1 => AppealLine.SlightlyWell, 2 => AppealLine.PrettyWell, _ => AppealLine.Excellently });
    }

    private void QualityDependsOnTiming()
    {
        int r = rng.Next(10);
        int appeal;
        if (r < 3) { appeal = 10; Say(mover, AppealLine.AppealNotVeryWell); }
        else if (r < 6) { appeal = 20; Say(mover, AppealLine.SlightlyWell2); }
        else if (r < 8) { appeal = 40; Say(mover, AppealLine.PrettyWell2); }
        else if (r < 9) { appeal = 60; Say(mover, AppealLine.VeryWell); }
        else { appeal = 80; Say(mover, AppealLine.Excellently2); }
        Mover.Appeal = appeal;
    }

    private void BetterIfSameType()
    {
        int turn = turnOrder[mover];
        int i = turn - 1;
        int j;
        if (turn == 0) return;
        while (true)
        {
            for (j = 0; j < Contestants; j++)
                if (turnOrder[j] == i) break;
            if (status[j].NoMoreTurns || status[j].Nervous || status[j].NumTurnsSkipped != 0)
            {
                if (--i < 0) return;
            }
            else
            {
                break;
            }
        }
        var move = Mover.CurrMove!.Contest;
        var theirs = status[j].CurrMove?.Contest.Category ?? ContestStat.Cool;
        if (move.Category == theirs)
        {
            Mover.Appeal += HoennContestDatabase.Effect(move.Effect).Appeal * 2;
            Say(mover, AppealLine.SameTypeGood);
        }
    }

    private void BetterIfDiffType()
    {
        if (turnOrder[mover] == 0) return;
        var move = Mover.CurrMove!.Contest;
        for (int i = 0; i < Contestants; i++)
        {
            var theirs = status[i].CurrMove?.Contest.Category ?? ContestStat.Cool;
            if (turnOrder[mover] - 1 == turnOrder[i] && move.Category != theirs)
            {
                Mover.Appeal += HoennContestDatabase.Effect(move.Effect).Appeal * 2;
                Say(mover, AppealLine.DiffTypeGood);
                break;
            }
        }
    }

    private void AffectedByPrevAppeal()
    {
        if (turnOrder[mover] == 0) return;
        for (int i = 0; i < Contestants; i++)
        {
            if (turnOrder[mover] - 1 == turnOrder[i])
            {
                if (Mover.Appeal > status[i].Appeal) { Mover.Appeal *= 2; Say(mover, AppealLine.StoodOutAsMuch); }
                else if (Mover.Appeal < status[i].Appeal) { Mover.Appeal = 0; Say(mover, AppealLine.NotAsWell); }
            }
        }
    }

    private void ImproveCondition()
    {
        if (Mover.Condition < MaxCondition)
        {
            Mover.Condition += 10;
            Mover.ConditionMod = ConditionChange.Gain;
            Say(mover, AppealLine.ConditionRose);
        }
        else
        {
            Say(mover, AppealLine.NoConditionImprove);
        }
    }

    private void NextAppealEarlier()
    {
        if (AppealNumber == Appeals - 1) return;
        var order = status.Select(s => s.NextTurnOrder).ToArray();
        order[mover] = NoOrder;
        for (int i = 0; i < Contestants; i++)
        {
            int j;
            for (j = 0; j < Contestants; j++)
            {
                if (j != mover && i == order[j] && order[j] == status[j].NextTurnOrder)
                {
                    order[j]++;
                    break;
                }
            }
            if (j == Contestants) break;
        }
        order[mover] = 0;
        for (int i = 0; i < Contestants; i++) status[i].NextTurnOrder = order[i];
        Mover.TurnOrderModAction = 1;
        Say(mover, AppealLine.MoveUpLine);
    }

    private void NextAppealLater()
    {
        if (AppealNumber == Appeals - 1) return;
        var order = status.Select(s => s.NextTurnOrder).ToArray();
        order[mover] = NoOrder;
        for (int i = Contestants - 1; i > -1; i--)
        {
            int j;
            for (j = 0; j < Contestants; j++)
            {
                if (j != mover && i == order[j] && order[j] == status[j].NextTurnOrder)
                {
                    order[j]--;
                    break;
                }
            }
            if (j == Contestants) break;
        }
        order[mover] = Contestants - 1;
        for (int i = 0; i < Contestants; i++) status[i].NextTurnOrder = order[i];
        Mover.TurnOrderModAction = 2;
        Say(mover, AppealLine.MoveBackLine);
    }

    private void ScrambleNextTurnOrder()
    {
        if (AppealNumber == Appeals - 1) return;
        var order = status.Select(s => s.NextTurnOrder).ToArray();
        var unselected = Enumerable.Range(0, Contestants).ToArray();
        for (int i = 0; i < Contestants; i++)
        {
            int r = rng.Next(Contestants - i);
            for (int j = 0; j < Contestants; j++)
            {
                if (unselected[j] != NoOrder)
                {
                    if (r == 0)
                    {
                        order[j] = i;
                        unselected[j] = NoOrder;
                        break;
                    }
                    r--;
                }
            }
        }
        for (int i = 0; i < Contestants; i++) status[i].NextTurnOrder = order[i];
        Mover.TurnOrderModAction = 3;
        Say(mover, AppealLine.ScrambleOrder);
    }

    private void BadlyStartleMonsWithGoodAppeals()
    {
        int numJammed = 0;
        for (int i = 0; i < Contestants; i++)
        {
            if (turnOrder[mover] > turnOrder[i])
            {
                if (status[i].Appeal > 0) jam = RoundUp(status[i].Appeal / 2);
                else jam = 10;
                jamQueue.Clear();
                jamQueue.Add(i);
                if (WasAtLeastOneOpponentJammed()) numJammed++;
            }
        }
        if (numJammed == 0) Say2(mover, AppealLine.MessedUp);
        Say(mover, AppealLine.AttemptStartle);
    }

    private void BetterWhenAudienceExcited()
    {
        int appeal;
        switch (ApplauseLevel)
        {
            case 0: appeal = 10; Say(mover, AppealLine.AppealNotVeryWell); break;
            case 1: appeal = 20; Say(mover, AppealLine.SlightlyWell2); break;
            case 2: appeal = 30; Say(mover, AppealLine.PrettyWell2); break;
            case 3: appeal = 50; Say(mover, AppealLine.VeryWell); break;
            default: appeal = 60; Say(mover, AppealLine.Excellently2); break;
        }
        Mover.Appeal = appeal;
    }

    // ---- shared helpers

    private bool CanUnnerve(int i)
    {
        var s = status[i];
        if (s.Immune)
        {
            Say(i, AppealLine.AvoidSeeing);
            return false;
        }
        if (s.JamSafetyCount != 0)
        {
            s.JamSafetyCount--;
            Say(i, AppealLine.AvertGaze);
            return false;
        }
        return !s.NoMoreTurns && s.NumTurnsSkipped == 0;
    }

    private void MakeNervous(int i)
    {
        status[i].Nervous = true;
        status[i].CurrMove = null;
    }

    private bool WasAtLeastOneOpponentJammed()
    {
        var buffer = new int[Contestants];
        foreach (int who in jamQueue)
        {
            if (!CanUnnerve(who)) continue;
            var s = status[who];
            jam2 = jam;
            if (s.MoreEasilyStartled) jam2 *= 2;
            if (s.Resistant)
            {
                jam2 = 10;
                Say(who, AppealLine.LittleDistracted);
            }
            else
            {
                jam2 -= s.JamReduction;
                if (jam2 <= 0)
                {
                    jam2 = 0;
                    Say(who, AppealLine.NotFazed);
                }
                else
                {
                    s.Appeal -= jam2;
                    s.Jam += jam2;
                    Say(who, StartledLine(jam2));
                    buffer[who] = jam2;
                }
            }
        }
        return buffer.Any(b => b != 0);
    }

    private static AppealLine StartledLine(int jam) => jam switch
    {
        >= 60 => AppealLine.TrippedOver,
        >= 40 => AppealLine.LeaptUp,
        >= 30 => AppealLine.UtterCry,
        >= 20 => AppealLine.TurnedBack,
        _ => AppealLine.LookedDown
    };

    private static int RoundTowardsZero(int score)
    {
        int abs = Math.Abs(score) % 10;
        if (score < 0)
        {
            if (abs != 0) score -= 10 - abs;
        }
        else
        {
            score -= abs;
        }
        return score;
    }

    private static int RoundUp(int score)
    {
        int abs = Math.Abs(score) % 10;
        if (abs != 0) score += 10 - abs;
        return score;
    }
}

/// <summary>The numbers that hold the whole of a Hoenn contest (plan 21 · H11), from Emerald's tables.</summary>
public static class AppealRules
{
    // sContestExcitementTable: how a move of a quality moves the crowd in a contest of another (+1 the contest's own)
    private static readonly int[,] ExcitementTable =
    {
        { +1, 0, -1, -1, 0 },
        { 0, +1, 0, -1, -1 },
        { -1, 0, +1, 0, -1 },
        { -1, -1, 0, +1, 0 },
        { 0, -1, -1, 0, +1 }
    };

    /// <summary>How a move of <paramref name="move"/>'s quality moves the crowd of a contest of <paramref name="contest"/>'s: +1, 0 or -1.</summary>
    public static int Excitement(ContestStat contest, ContestStat move) => ExcitementTable[(int)contest, (int)move];

    /// <summary>The hearts a score is shown as: so many tens of points, held to eight (<c>MAX_CONTEST_MOVE_HEARTS</c>).</summary>
    public static int HeartsOf(int appeal) => Math.Clamp(appeal / 10, 0, 8);

    /// <summary>The points a Master Rank winner needs for the painter to come (<c>ShouldReadyContestArtist</c>).</summary>
    public const int PaintingPoints = 800;
}
