using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where a Super Contest has got to on screen.</summary>
public enum SuperContestPhase
{
    /// <summary>The four contestants are announced, one after another.</summary>
    Lineup,
    /// <summary>The Visual Competition's theme is given.</summary>
    Theme,
    /// <summary>Sixty seconds to dress the Pokémon up from the Fashion Case.</summary>
    Dressing,
    /// <summary>Each contestant's look in turn, with the hearts it is worth.</summary>
    Looks,
    /// <summary>The Dance Competition is announced, and before each lead's turn who leads.</summary>
    DanceLead,
    /// <summary>The dance itself, a frame every thirtieth of a second.</summary>
    Dance,
    /// <summary>The dance's points.</summary>
    DanceScores,
    /// <summary>The Acting Competition is announced.</summary>
    ActingStart,
    /// <summary>The player chooses a move, then the judge to show it to.</summary>
    ActingMove,
    ActingJudge,
    /// <summary>The turn plays, a thing at a time.</summary>
    ActingShow,
    /// <summary>The acting's points once its four turns are over.</summary>
    ActingScores,
    /// <summary>The results' board: each round's bars, then the winner.</summary>
    Results,
    /// <summary>The winner's ribbon.</summary>
    Award,
    /// <summary>A round held for practice is over: how the player did.</summary>
    PracticeResult,
    Done
}

/// <summary>
/// A Super Contest on screen (plan 06 · R17a; style guide, "Super Contests"): the contestants announced, the Pokémon
/// dressed up from the Fashion Case against the clock, the dance stepped with the arrows, the moves and judges chosen
/// turn by turn, and the results' board, on the beats of the original's contest (<c>scripts_contests.s</c>,
/// <c>overlay017</c>, <c>overlay022</c>). The rules are <see cref="SuperContest"/>'s, <see cref="SuperContestDance"/>'s
/// and <see cref="SuperContestActing"/>'s; this screen is their clock and their buttons. Everything here is a
/// function of <see cref="Advance"/> and the buttons (<see cref="PressConfirm"/>, <see cref="PressCancel"/>,
/// <see cref="PressStart"/>, <see cref="Move"/>, <see cref="PressStep"/>), so tests hold a contest without a window;
/// <see cref="Update"/> reads the keys and <see cref="Draw"/> draws. The lines are our own.
/// </summary>
public sealed class SuperContestScreen
{
    /// <summary>The Fashion Case's grid is this many across.</summary>
    public const int CaseColumns = 6;

    /// <summary>A line of the acting waits this long for a button before going on by itself, and a look, the scores and a board's bar as long.</summary>
    public const float LineWait = 2.4f, LookWait = 3.2f, BarTime = 1.4f, LeadTime = 2.2f;

    private const float Tick = 1f / SuperContestDance.FramesPerSecond;

    public bool IsActive { get; private set; }
    public SuperContestPhase Phase { get; private set; } = SuperContestPhase.Done;
    public float PhaseTime { get; private set; }

    /// <summary>The contest being held, and its dance and its acting once they have begun.</summary>
    public SuperContest? Contest { get; private set; }
    public SuperContestDance? Dance { get; private set; }
    public SuperContestActing? Acting { get; private set; }

    /// <summary>The line in the announcer's box.</summary>
    public string Line { get; private set; } = "";

    /// <summary>Which contestant the line-up or the looks are showing (entry number 1, the fourth of the four, first); -1 while the line-up welcomes everyone.</summary>
    public int Showing { get; private set; }

    // ---- the dressing-up
    /// <summary>What the Pokémon wears so far, the cursor on the case's grid, and the seconds left.</summary>
    public IReadOnlyList<int> Worn => worn;
    private readonly List<int> worn = new();
    public int Cursor { get; private set; }
    public float TimeLeft { get; private set; }
    public int Limit => Contest == null ? 0 : ContestRules.AccessoryLimit(Contest.Rank);

    /// <summary>The case's kinds with how many are still in it (those worn taken out), in the case's order.</summary>
    public IReadOnlyList<(int Accessory, int Count)> InCase => inCase;
    private List<(int Accessory, int Count)> inCase = new();

    // ---- the dance
    /// <summary>The part of a dance frame since the last, 0 to 1, and the last steps danced with how long ago, for the picture.</summary>
    public float Between => carry / Tick;
    public IReadOnlyList<(DanceMark Mark, float Age)> Marks => marks;
    private readonly List<(DanceMark Mark, float Age)> marks = new();
    private float carry;

    /// <summary>The arrow a tool holds down for the dance in place of the keys (the screenshot harness), for one frame of the dance.</summary>
    public DanceStep? Held { get; set; }

    // ---- the acting
    public int MoveCursor { get; private set; }
    public int JudgeCursor { get; private set; }

    /// <summary>The turn being shown: its events, the one on screen, and how long it has been there.</summary>
    public IReadOnlyList<ActingEvent> Events { get; private set; } = Array.Empty<ActingEvent>();
    public int EventIndex { get; private set; }
    public float EventTime { get; private set; }
    public ActingEvent? Event => EventIndex >= 0 && EventIndex < Events.Count ? Events[EventIndex] : null;

    /// <summary>What the stage shows of the turn so far: each judge's voltage, each contestant's points of the turn, what each chose.</summary>
    public int[] ShownVoltage { get; } = new int[ContestRules.Judges];
    public int[] TurnPoints { get; } = new int[ContestRules.Contestants];
    public int[] ShownTotal { get; } = new int[ContestRules.Contestants];
    public ActingChoice?[] Chosen { get; } = new ActingChoice?[ContestRules.Contestants];
    public int[] ShownOrder { get; private set; } = { 0, 1, 2, 3 };

    /// <summary>The turn the stage is showing (the rules are a turn ahead while one plays), and which of the player's moves could be shown in it.</summary>
    public int ShownTurn { get; private set; }
    public bool[] Usable { get; } = new bool[4];

    // ---- the results
    /// <summary>The board's stage: 0 to 2 a round's bars growing, 3 the winner.</summary>
    public int ResultStage { get; private set; }

    /// <summary>How the contest came out, once it has ended (<see cref="ContestAwards.Conclude"/>, done as the board names the winner).</summary>
    public ContestOutcome? Outcome { get; private set; }

    private FashionCase fashionCase = new();
    private readonly Queue<string> lines = new();

    /// <summary>
    /// Opens a contest: the player's Pokémon of the team against three of the original's contestants, drawn on
    /// <paramref name="chance"/>. <paramref name="afterGame"/> is the original's "game completed and National Pokédex
    /// obtained".
    /// </summary>
    public void Begin(Pokemon pokemon, string trainer, bool woman, ContestStat category, ContestRank rank, ContestMode mode, bool afterGame, FashionCase accessories, Random chance)
    {
        Contest = new SuperContest(pokemon, trainer, woman, category, rank, mode, afterGame, chance);
        fashionCase = accessories;
        Dance = null;
        Acting = null;
        Outcome = null;
        IsActive = true;
        IsShown = true;
        worn.Clear();
        marks.Clear();
        lines.Clear();
        Array.Clear(ShownVoltage);
        Array.Clear(TurnPoints);
        Array.Clear(ShownTotal);
        Array.Clear(Chosen);
        Events = Array.Empty<ActingEvent>();
        Held = null;
        Showing = -1;
        Enter(SuperContestPhase.Lineup);
        Line = Contest.IsPractice
            ? Text.Of("contest.practiceWelcome", ("round", RoundName(mode)))
            : Text.Of("contest.welcome", ("rank", Text.Of($"contest.rank.{rank}")), ("category", Text.Of($"contest.category.{category}")));
        AudioManager.PlaySound("menu_open");
    }

    /// <summary>
    /// Whether there is still a picture to show: a contest that has ended keeps its last board up while the game
    /// fades back to the field, until <see cref="Close"/>.
    /// </summary>
    public bool IsShown { get; private set; }

    /// <summary>Puts the screen away without anything more (the game is leaving it).</summary>
    public void Close()
    {
        IsActive = false;
        IsShown = false;
        Phase = SuperContestPhase.Done;
    }

    private static string RoundName(ContestMode mode) => mode switch
    {
        ContestMode.PracticeVisual => Text.Of("contest.competition.visual"),
        ContestMode.PracticeDance => Text.Of("contest.competition.dance"),
        _ => Text.Of("contest.competition.acting")
    };

    private void Enter(SuperContestPhase phase)
    {
        Phase = phase;
        PhaseTime = 0f;
    }

    private string Who(int contestant)
    {
        var c = Contest!.Contestants[contestant];
        return Text.Of("contest.whose", ("trainer", c.Trainer), ("pokemon", c.Pokemon.DisplayName));
    }

    // ---------------------------------------------------------------- the buttons

    /// <summary>The confirm button: on past a line, an accessory put on, a step of a choice taken.</summary>
    public void PressConfirm()
    {
        if (!IsActive || Contest == null) return;
        switch (Phase)
        {
            case SuperContestPhase.Lineup:
                NextInLineup();
                break;
            case SuperContestPhase.Theme:
                StartDressing();
                break;
            case SuperContestPhase.Dressing:
                PutOn();
                break;
            case SuperContestPhase.Looks:
                NextLook();
                break;
            case SuperContestPhase.DanceLead when Dance == null:
                BeginDance();
                break;
            case SuperContestPhase.DanceScores:
                AfterDance();
                break;
            case SuperContestPhase.ActingStart:
                AskMove();
                break;
            case SuperContestPhase.ActingMove:
                ChooseMove();
                break;
            case SuperContestPhase.ActingJudge:
                ChooseJudge();
                break;
            case SuperContestPhase.ActingShow:
                NextEvent();
                break;
            case SuperContestPhase.ActingScores:
                AfterActing();
                break;
            case SuperContestPhase.Results:
                NextResult();
                break;
            case SuperContestPhase.Award:
                NextAwardLine();
                break;
            case SuperContestPhase.PracticeResult:
                End();
                break;
        }
    }

    /// <summary>The cancel button: the last accessory off, back from the judges to the moves; elsewhere as confirm.</summary>
    public void PressCancel()
    {
        if (!IsActive) return;
        switch (Phase)
        {
            case SuperContestPhase.Dressing:
                TakeOff();
                break;
            case SuperContestPhase.ActingJudge:
                Enter(SuperContestPhase.ActingMove);
                Line = MoveQuestion();
                AudioManager.PlaySound("cancel");
                break;
            case SuperContestPhase.ActingMove:
                break;
            default:
                PressConfirm();
                break;
        }
    }

    /// <summary>The start button: the dressing-up is done.</summary>
    public void PressStart()
    {
        if (IsActive && Phase == SuperContestPhase.Dressing) FinishDressing();
    }

    /// <summary>Moves the cursor of whatever is being chosen: the case's grid, the moves, the judges.</summary>
    public void Move(int dx, int dy)
    {
        if (!IsActive || (dx == 0 && dy == 0)) return;
        switch (Phase)
        {
            case SuperContestPhase.Dressing when inCase.Count > 0:
            {
                int next = UiNav.Grid(Cursor, inCase.Count, CaseColumns, dx, dy);
                if (next != Cursor) AudioManager.PlaySound("cursor");
                Cursor = next;
                break;
            }
            case SuperContestPhase.ActingMove:
            {
                int count = Contest!.Player.Pokemon.Moves.Count;
                MoveCursor = UiNav.Wrap(MoveCursor, dy != 0 ? dy : dx, count);
                AudioManager.PlaySound("cursor");
                break;
            }
            case SuperContestPhase.ActingJudge:
                JudgeCursor = UiNav.Wrap(JudgeCursor, dx != 0 ? dx : dy, ContestRules.Judges);
                AudioManager.PlaySound("cursor");
                break;
        }
    }

    /// <summary>A step of the dance, for the frame to come. False when the Pokémon can't take one now.</summary>
    public bool PressStep(DanceStep step)
    {
        if (!IsActive || Phase != SuperContestPhase.Dance || Dance == null) return false;
        return Dance.Press(step);
    }

    // ---------------------------------------------------------------- the line-up

    // The contestants are announced by their entry numbers, 1 to 4: the player is the fourth (IntroduceFirstContestant…)
    private void NextInLineup()
    {
        var contest = Contest!;
        // The welcome first (nobody singled out), then entry number 1, who is the fourth of the four, down to the player
        if (Showing != 0)
        {
            Announce(Showing < 0 ? ContestRules.Contestants - 1 : Showing - 1);
            return;
        }
        AudioManager.PlaySound("select");
        if (contest.HasVisual)
        {
            Enter(SuperContestPhase.Theme);
            Line = Text.Of("contest.themeLine", ("theme", SuperContestDatabase.Themes[contest.Theme].Name), ("seconds", ContestRules.DressingSeconds), ("limit", Limit));
        }
        else if (contest.HasDance) AnnounceDance();
        else AnnounceActing();
    }

    private void Announce(int contestant)
    {
        var c = Contest!.Contestants[contestant];
        Showing = contestant;
        PhaseTime = 0f;
        Line = Text.Of("contest.entry", ("n", SuperContest.EntryNumber(contestant)), ("trainer", c.Trainer), ("pokemon", c.Pokemon.DisplayName));
        AudioManager.PlaySound("select");
    }

    // ---------------------------------------------------------------- the Visual Competition

    private void StartDressing()
    {
        worn.Clear();
        Cursor = 0;
        TimeLeft = ContestRules.DressingSeconds;
        RefreshCase();
        Enter(SuperContestPhase.Dressing);
        Line = inCase.Count == 0 ? Text.Of("contest.emptyCase") : "";
        AudioManager.PlaySound("select");
    }

    private void RefreshCase()
    {
        inCase = fashionCase.Held.Select(h => (h.Accessory, h.Count - worn.Count(w => w == h.Accessory))).ToList();
        Cursor = Math.Clamp(Cursor, 0, Math.Max(0, inCase.Count - 1));
    }

    private void PutOn()
    {
        if (inCase.Count == 0 || worn.Count >= Limit || inCase[Cursor].Count <= 0)
        {
            AudioManager.PlaySound("error");
            return;
        }
        worn.Add(inCase[Cursor].Accessory);
        RefreshCase();
        AudioManager.PlaySound("select");
    }

    private void TakeOff()
    {
        if (worn.Count == 0) return;
        worn.RemoveAt(worn.Count - 1);
        RefreshCase();
        AudioManager.PlaySound("cancel");
    }

    private void FinishDressing()
    {
        Contest!.JudgeVisual(worn);
        Showing = 3;
        Enter(SuperContestPhase.Looks);
        Line = LookLine(3);
        AudioManager.PlaySound("page");
    }

    private string LookLine(int contestant)
    {
        var c = Contest!.Contestants[contestant];
        return Text.Of("contest.look", ("n", SuperContest.EntryNumber(contestant)), ("who", Who(contestant)), ("hearts", Hearts(c.VisualHearts)));
    }

    private static string Hearts(int hearts) => Text.Of("contest.hearts", ("n", hearts));

    private void NextLook()
    {
        if (Showing > 0)
        {
            Showing--;
            PhaseTime = 0f;
            Line = LookLine(Showing);
            AudioManager.PlaySound("page");
            return;
        }
        if (Contest!.HasDance) AnnounceDance();
        else Finish();
    }

    // ---------------------------------------------------------------- the Dance Competition

    private void AnnounceDance()
    {
        Dance = null;
        marks.Clear();
        Enter(SuperContestPhase.DanceLead);
        Line = Text.Of("contest.danceIntro");
        AudioManager.PlaySound("select");
    }

    private void BeginDance()
    {
        Dance = new SuperContestDance(Contest!);
        carry = 0f;
        LeadIn();
    }

    private void LeadIn()
    {
        Enter(SuperContestPhase.DanceLead);
        int lead = Dance!.Lead;
        Line = lead == 0 ? Text.Of("contest.youLead", ("name", Contest!.Player.Pokemon.DisplayName)) : Text.Of("contest.otherLeads", ("name", Who(lead)));
        AudioManager.PlaySound("select");
    }

    // A frame of the dance: the step held is pressed, the rules step, and what was danced is kept a moment for the picture
    private void DanceFrame()
    {
        var dance = Dance!;
        if (Held is { } held)
        {
            dance.Press(held);
            Held = null;
        }
        int rotation = dance.Rotation;
        dance.Advance();
        // A tick on each beat, the downbeat of each half a little louder
        if (dance.Frame > 0 && !dance.IsOver)
        {
            long beat = dance.Song.Beat, at = dance.Frame * 10000L;
            if (at / beat != (at - 10000L) / beat || dance.Frame == 1) AudioManager.PlaySound("poketch_count");
        }
        foreach (var mark in dance.Danced)
        {
            marks.Add((mark, 0f));
            if (mark.Contestant == 0) AudioManager.PlaySound(mark.Missed ? "error" : mark.Excellent ? "stat_up" : "select");
        }
        if (dance.IsOver)
        {
            Enter(SuperContestPhase.DanceScores);
            Line = Text.Of("contest.danceOver", ("name", Contest!.Player.Pokemon.DisplayName), ("points", dance.Score[0]));
            AudioManager.PlaySound("levelup");
        }
        else if (dance.Rotation != rotation) LeadIn();
    }

    private void AfterDance()
    {
        if (Contest!.HasActing) AnnounceActing();
        else Finish();
    }

    // ---------------------------------------------------------------- the Acting Competition

    private void AnnounceActing()
    {
        Acting = new SuperContestActing(Contest!);
        ShownOrder = Acting.Order.ToArray();
        Array.Clear(ShownVoltage);
        Array.Clear(TurnPoints);
        Array.Clear(ShownTotal);
        MarkUsable();
        Array.Clear(Chosen);
        Enter(SuperContestPhase.ActingStart);
        Line = Text.Of("contest.actingIntro");
        AudioManager.PlaySound("select");
    }

    private void MarkUsable()
    {
        var moves = Contest!.Player.Pokemon.Moves;
        ShownTurn = Acting!.Turn;
        for (int m = 0; m < Usable.Length; m++) Usable[m] = m < moves.Count && Acting.CanUse(0, moves[m].Data);
    }

    private string MoveQuestion() => Text.Of("contest.moveQuestion", ("turn", Acting!.Turn + 1), ("turns", SuperContestActing.Turns), ("name", Contest!.Player.Pokemon.DisplayName));

    private void AskMove()
    {
        var acting = Acting!;
        ShownOrder = acting.Order.ToArray();
        MarkUsable();
        Array.Clear(TurnPoints);
        Array.Clear(Chosen);
        var moves = Contest!.Player.Pokemon.Moves;
        if (MoveCursor >= moves.Count || !acting.CanUse(0, moves[MoveCursor].Data))
            MoveCursor = Math.Max(0, moves.FindIndex(m => acting.CanUse(0, m.Data)));
        Enter(SuperContestPhase.ActingMove);
        Line = MoveQuestion();
    }

    private void ChooseMove()
    {
        var move = Contest!.Player.Pokemon.Moves[MoveCursor].Data;
        if (!Acting!.CanUse(0, move))
        {
            AudioManager.PlaySound("error");
            return;
        }
        Enter(SuperContestPhase.ActingJudge);
        Line = Text.Of("contest.judgeQuestion", ("move", move.Name));
        AudioManager.PlaySound("select");
    }

    private void ChooseJudge()
    {
        var acting = Acting!;
        var choices = acting.Choose(new ActingChoice(Contest!.Player.Pokemon.Moves[MoveCursor].Data, JudgeCursor));
        ShownOrder = acting.Order.ToArray();
        Array.Copy(acting.Voltage, ShownVoltage, ShownVoltage.Length);
        Events = acting.Perform(choices);
        for (int c = 0; c < choices.Length; c++) Chosen[c] = null;
        pending = choices;
        EventIndex = -1;
        Enter(SuperContestPhase.ActingShow);
        AudioManager.PlaySound("select");
        NextEvent();
    }

    private ActingChoice[] pending = Array.Empty<ActingChoice>();

    // The next thing of the turn comes on stage: its line, and what it changes of the picture
    private void NextEvent()
    {
        var contest = Contest!;
        EventIndex++;
        EventTime = 0f;
        if (Event is not { } e)
        {
            EndTurn();
            return;
        }
        string who = e.Contestant >= 0 ? contest.Contestants[e.Contestant].Pokemon.DisplayName : "";
        string judge = e.Judge >= 0 ? contest.Judges[e.Judge].Name : "";
        switch (e.Step)
        {
            case ActingStep.Perform:
                Chosen[e.Contestant] = pending[e.Contestant];
                // A voltage that was filled by the act before starts again from nothing
                for (int j = 0; j < ShownVoltage.Length; j++)
                    if (ShownVoltage[j] >= ContestRules.FullVoltage) ShownVoltage[j] = 0;
                TurnPoints[e.Contestant] = e.Score;
                Line = Text.Of(e.Flag ? "contest.showedTaken" : "contest.showed", ("who", who), ("judge", judge), ("move", e.Move!.Name));
                AudioManager.PlaySound("hit_normal");
                break;
            case ActingStep.Effect or ActingStep.LateEffect:
                TurnPoints[e.Contestant] = e.Score;
                Line = EffectLine(e, who);
                AudioManager.PlaySound(e.Points > 0 ? "stat_up" : "select");
                break;
            case ActingStep.Doubled:
                TurnPoints[e.Contestant] = e.Score;
                Line = Text.Of("contest.doubled", ("who", who));
                AudioManager.PlaySound("stat_up");
                break;
            case ActingStep.Voltage:
                TurnPoints[e.Contestant] = e.Score;
                ShownVoltage[e.Judge] = e.Voltage;
                if (e.Change == 0 && e.Points == 0)
                {
                    // Nothing to tell: on to the next thing at once
                    NextEvent();
                    return;
                }
                Line = e.Points > 0 ? Text.Of("contest.peak", ("judge", judge), ("who", who), ("hearts", Hearts(e.Points / ContestRules.PointsPerHeart)))
                    : e.Change > 0 ? Text.Of("contest.voltageRose", ("judge", judge))
                    : Text.Of("contest.voltageFell", ("judge", judge));
                AudioManager.PlaySound(e.Points > 0 ? "levelup" : e.Change > 0 ? "stat_up" : "stat_down");
                break;
            case ActingStep.Judge:
                foreach (int c in e.Others) TurnPoints[c] += e.Points;
                Line = e.Others.Count switch
                {
                    1 => Text.Of("contest.impressed", ("judge", judge), ("hearts", Hearts(e.Points / ContestRules.PointsPerHeart)), ("name", contest.Contestants[e.Others[0]].Pokemon.DisplayName)),
                    4 => Text.Of("contest.nobodyImpressed", ("judge", judge)),
                    _ => Text.Of("contest.shared", ("judge", judge), ("count", e.Others.Count), ("hearts", Hearts(e.Points / ContestRules.PointsPerHeart)))
                };
                AudioManager.PlaySound("select");
                break;
            case ActingStep.TurnEnd:
                EndTurn();
                break;
        }
    }

    // An effect's line, in our own words on the original's beats (contest_effects.json's messages by effect and variant)
    private string EffectLine(ActingEvent e, string who)
    {
        string other = e.Other >= 0 ? Contest!.Contestants[e.Other].Pokemon.DisplayName : "";
        string hearts = Hearts(e.Points / ContestRules.PointsPerHeart);
        return e.Effect switch
        {
            ContestEffect.FirstNextTurn => Say(e.Variant == 1 ? "contest.effect.firstFrom" : "contest.effect.first"),
            ContestEffect.LastNextTurn => Say(e.Variant == 1 ? "contest.effect.lastFrom" : "contest.effect.last"),
            ContestEffect.DoubledJudge => Say(e.Points > 0 ? "contest.effect.sharedJudge" : "contest.effect.judgeAlone"),
            ContestEffect.TwoHeartsWhenVoltageUp => Say("contest.effect.raised"),
            ContestEffect.UniqueJudge => Say(e.Variant == 0 ? "contest.effect.unique" : "contest.effect.notUnique"),
            ContestEffect.ConsecutiveUse => Say("contest.effect.again"),
            ContestEffect.Voltage => Say("contest.effect.added"),
            ContestEffect.AllSameJudge => Say(e.Variant == 0 ? "contest.effect.allSame" : "contest.effect.notAllSame"),
            ContestEffect.LowersVoltage => Say("contest.effect.cooled"),
            ContestEffect.StealVoltage => Say("contest.effect.stole"),
            ContestEffect.SuppressVoltage => Say("contest.effect.noRise"),
            ContestEffect.RandomOrder => Say("contest.effect.shuffled"),
            ContestEffect.DoubleFinalAct => Say("contest.effect.closed"),
            ContestEffect.LowVoltageAdvantage => Say(e.Points > 0 ? "contest.effect.lowVoltage" : "contest.effect.tooHigh"),
            ContestEffect.FirstPerformanceAdvantage => Say("contest.effect.opened"),
            ContestEffect.FinalPerformanceAdvantage => Say("contest.effect.closed"),
            ContestEffect.NoVoltageDown => Say("contest.effect.noFall"),
            ContestEffect.TwoVoltageInARowAdvantage => Say("contest.effect.twiceRose"),
            ContestEffect.HighScoreLaterTurn => Say("contest.effect.performed"),
            ContestEffect.MaxVoltageAdvantage => Say("contest.effect.followedPeak"),
            ContestEffect.PityPoints => Say("contest.effect.least"),
            _ => ""
        };

        string Say(string key) => Text.Of(key, ("who", who), ("other", other), ("hearts", hearts), ("place", Place(e.Variant)));
    }

    private static string Place(int place)
    {
        int at = place is >= 0 and <= 2 ? place : 3;
        return Text.Of($"contest.turnPlace.{at}");
    }

    private void EndTurn()
    {
        var acting = Acting!;
        Array.Copy(acting.Total, ShownTotal, ShownTotal.Length);
        Array.Copy(acting.Voltage, ShownVoltage, ShownVoltage.Length);
        Events = Array.Empty<ActingEvent>();
        EventIndex = 0;
        if (!acting.IsOver)
        {
            AskMove();
            return;
        }
        Enter(SuperContestPhase.ActingScores);
        Line = Text.Of("contest.judgingOver", ("name", Contest!.Player.Pokemon.DisplayName), ("hearts", Hearts(acting.Total[0] / ContestRules.PointsPerHeart)));
        AudioManager.PlaySound("levelup");
    }

    private void AfterActing() => Finish();

    // ---------------------------------------------------------------- the results

    private void Finish()
    {
        var contest = Contest!;
        contest.Finish();
        if (contest.IsPractice)
        {
            Outcome = ContestAwards.Conclude(contest.Player.Pokemon, contest.Category, contest.Rank, contest.Mode, contest.Player.Placement, fashionCase);
            Enter(SuperContestPhase.PracticeResult);
            Line = contest.Player.Placement switch
            {
                0 => Text.Of("contest.practiceResult.best"),
                1 => Text.Of("contest.practiceResult.second"),
                _ => Text.Of("contest.practiceResult.lower", ("count", contest.Player.Placement))
            };
            AudioManager.PlaySound(contest.Player.Placement <= 1 ? "levelup" : "select");
            return;
        }
        ResultStage = 0;
        Enter(SuperContestPhase.Results);
        Line = Text.Of("contest.resultsIn");
        AudioManager.PlaySound("page");
    }

    private void NextResult()
    {
        var contest = Contest!;
        if (ResultStage < 3 && PhaseTime < BarTime)
        {
            // A press hurries the bar to its end
            PhaseTime = BarTime;
            return;
        }
        if (ResultStage < 2)
        {
            ResultStage++;
            PhaseTime = 0f;
            Line = Text.Of(ResultStage == 1 ? "contest.resultsDance" : "contest.resultsActing");
            AudioManager.PlaySound("page");
            return;
        }
        if (ResultStage == 2)
        {
            ResultStage = 3;
            PhaseTime = 0f;
            int winner = contest.Contestants.ToList().IndexOf(contest.Winner);
            Outcome = ContestAwards.Conclude(contest.Player.Pokemon, contest.Category, contest.Rank, contest.Mode, contest.Player.Placement, fashionCase);
            Line = Text.Of("contest.winner", ("n", SuperContest.EntryNumber(winner)), ("trainer", contest.Winner.Trainer), ("pokemon", contest.Winner.Pokemon.DisplayName));
            AudioManager.PlaySound("levelup");
            return;
        }
        // The award: the winner's ribbon when it is the player's and new to the Pokémon (Contests_AwardCeremony)
        lines.Clear();
        if (Outcome is { Won: true } won)
        {
            if (won.NewRibbon != null) lines.Enqueue(Text.Of("contest.received", ("name", won.Pokemon), ("ribbon", won.NewRibbon)));
            else lines.Enqueue(Text.Of("contest.wearsAlready", ("name", won.Pokemon)));
        }
        else lines.Enqueue(Text.Of("contest.came", ("name", contest.Player.Pokemon.DisplayName), ("place", Rank(contest.Player.Placement))));
        Enter(SuperContestPhase.Award);
        NextAwardLine();
    }

    private static string Rank(int placement)
    {
        int at = placement is >= 0 and <= 2 ? placement : 3;
        return Text.Of($"contest.placeWord.{at}");
    }

    private void NextAwardLine()
    {
        if (lines.Count == 0)
        {
            End();
            return;
        }
        Line = lines.Dequeue();
        PhaseTime = 0f;
        AudioManager.PlaySound(Outcome is { NewRibbon: not null } ? "levelup" : "select");
    }

    private void End()
    {
        IsActive = false;
        Line = "";
        Enter(SuperContestPhase.Done);
    }

    // ---------------------------------------------------------------- time

    /// <summary>Time goes on: the dressing-up's clock runs down, the dance steps a frame each thirtieth of a second, a line of the acting gives way to the next.</summary>
    public void Advance(float dt)
    {
        if (!IsActive || dt <= 0f) return;
        PhaseTime += dt;
        for (int i = marks.Count - 1; i >= 0; i--)
        {
            marks[i] = (marks[i].Mark, marks[i].Age + dt);
            if (marks[i].Age > 1.2f) marks.RemoveAt(i);
        }

        switch (Phase)
        {
            case SuperContestPhase.Dressing:
                TimeLeft -= dt;
                if (TimeLeft <= 0f)
                {
                    TimeLeft = 0f;
                    FinishDressing();
                }
                break;
            case SuperContestPhase.DanceLead when Dance != null:
                if (PhaseTime >= LeadTime)
                {
                    Line = Dance.Lead == 0
                        ? Text.Of("contest.yourLead", ("steps", Dance.Song.Steps))
                        : Text.Of("contest.watchLead");
                    Enter(SuperContestPhase.Dance);
                }

                break;
            case SuperContestPhase.Dance:
                carry += dt;
                while (carry >= Tick && Phase == SuperContestPhase.Dance)
                {
                    carry -= Tick;
                    DanceFrame();
                }
                break;
            case SuperContestPhase.ActingShow:
                EventTime += dt;
                if (EventTime >= LineWait) NextEvent();
                break;
        }
    }

    // ---------------------------------------------------------------- the keys and the picture

    /// <summary>A frame of the game: the arrows move the cursor or dance a step, the buttons go on, and time passes.</summary>
    public void Update(float dt)
    {
        if (!IsActive) return;
        if (Phase == SuperContestPhase.Dance)
        {
            if (InputManager.IsActionPressed(GameAction.Up)) PressStep(DanceStep.Jump);
            else if (InputManager.IsActionPressed(GameAction.Down)) PressStep(DanceStep.Forward);
            else if (InputManager.IsActionPressed(GameAction.Left)) PressStep(DanceStep.Left);
            else if (InputManager.IsActionPressed(GameAction.Right)) PressStep(DanceStep.Right);
        }
        else
        {
            int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
            if (dx != 0 || dy != 0) Move(dx, dy);
            if (InputManager.IsActionPressed(GameAction.Confirm)) PressConfirm();
            else if (InputManager.IsActionPressed(GameAction.Cancel)) PressCancel();
            else if (InputManager.IsActionPressed(GameAction.Menu)) PressStart();
        }
        Advance(dt);
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsShown) return;
        ModernUi.DrawSuperContest(screenWidth, screenHeight, this);
    }
}
