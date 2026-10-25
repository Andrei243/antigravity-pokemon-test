using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Contest;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where a Hoenn contest has got to on screen.</summary>
public enum AppealPhase
{
    /// <summary>The four contestants are announced one after another, each with its condition's points.</summary>
    Lineup,
    /// <summary>The player chooses a move for the round.</summary>
    Choose,
    /// <summary>The round's turns play, a line at a time.</summary>
    Turn,
    /// <summary>A round is over: what each won, and the order of the next.</summary>
    RoundEnd,
    /// <summary>The scores of the whole contest, round by round, and the winner.</summary>
    Results,
    /// <summary>What the win brings: the ribbon, a Luxury Ball, the painter.</summary>
    Award,
    Done
}

/// <summary>
/// A Hoenn contest on screen (plan 21 · H11; style guide, "Hoenn's contests"): the four contestants announced with
/// their condition's points, then five rounds of choosing a move and watching the four appeals in turn, hearts won and
/// lost, the judge's eye, the crowd's meter, and the results' board. The rules are <see cref="AppealRound"/>'s; this
/// screen is their clock and their buttons. Everything here is a function of <see cref="Advance"/> and the buttons
/// (<see cref="PressConfirm"/>, <see cref="PressCancel"/>, <see cref="Move"/>), so tests hold a contest without a window;
/// <see cref="Update"/> reads the keys and <see cref="Draw"/> draws. The lines are our own, on Emerald's beats.
/// </summary>
public sealed class AppealScreen
{
    /// <summary>A line waits this long for a button before going on by itself.</summary>
    public const float LineWait = 2.6f;

    public bool IsActive { get; private set; }
    public bool IsShown { get; private set; }
    public AppealPhase Phase { get; private set; } = AppealPhase.Done;
    public float PhaseTime { get; private set; }

    /// <summary>The contest's rules, the stage and the player on it.</summary>
    public AppealRound? Round { get; private set; }
    public int Player { get; private set; }
    public bool Practice { get; private set; }

    /// <summary>The line in the announcer's box.</summary>
    public string Line { get; private set; } = "";

    /// <summary>The contestant the line-up is showing (-1 while it welcomes everyone).</summary>
    public int Showing { get; private set; } = -1;

    public int MoveCursor { get; private set; }

    /// <summary>The contestant whose turn is on screen, and the move it used.</summary>
    public int Mover { get; private set; } = -1;
    public string MoveShown { get; private set; } = "";

    /// <summary>What the stage shows so far: each contestant's hearts of the round in points, the points before the round, the stars, the judge's eye and the crowd's meter.</summary>
    public int[] ShownAppeal { get; } = new int[AppealRound.Contestants];
    public int[] ShownTotal { get; } = new int[AppealRound.Contestants];
    public int[] ShownStars { get; } = new int[AppealRound.Contestants];
    public bool[] ShownEye { get; } = new bool[AppealRound.Contestants];
    public bool[] ShownNervous { get; } = new bool[AppealRound.Contestants];
    public int ShownCrowd { get; private set; }

    /// <summary>The turn order the stage shows (contestant to place), for the round on screen and the next once it is over.</summary>
    public int[] ShownOrder { get; private set; } = { 0, 1, 2, 3 };

    /// <summary>How the contest came out, once it has ended.</summary>
    public AppealOutcome? Outcome { get; private set; }

    /// <summary>The results' board: 0 the round-one points, 1 the appeals', 2 the total and the places.</summary>
    public int ResultStage { get; private set; }

    /// <summary>The player's move that was chosen for the round and the effect's line of the cursor.</summary>
    public AppealMove? CurrentMove => Round != null && Player >= 0 && MoveCursor < Round.Appealers[Player].Moves.Count ? Round.Appealers[Player].Moves[MoveCursor] : null;

    private readonly Queue<(string Text, AppealEvent? Event)> lines = new();
    private readonly Queue<string> awardLines = new();
    private int[] totalsAtRoundStart = new int[AppealRound.Contestants];
    private int roundShown;
    private float wait;

    /// <summary>The round on screen, 1 to 5.</summary>
    public int RoundNumber => Round == null ? 0 : Math.Min(AppealRound.Appeals, roundShown + 1);

    /// <summary>Opens a contest for a Pokémon of the team, against three contestants drawn on <paramref name="chance"/>.</summary>
    public void Begin(Pokemon pokemon, string trainer, ContestStat category, AppealRank rank, bool afterTheGame, Random chance, bool practice = false)
    {
        var stage = HoennContestRules.Stage(pokemon, trainer, category, rank, afterTheGame, chance);
        Begin(new AppealRound(stage, category, rank, chance), practice);
    }

    /// <summary>Opens a contest on rules already made (tests, the screenshot harness).</summary>
    public void Begin(AppealRound round, bool practice = false)
    {
        Round = round;
        Player = round.PlayerIndex;
        Practice = practice;
        Outcome = null;
        IsActive = true;
        IsShown = true;
        lines.Clear();
        awardLines.Clear();
        Array.Clear(ShownAppeal);
        Array.Clear(ShownTotal);
        Array.Clear(ShownStars);
        Array.Clear(ShownEye);
        Array.Clear(ShownNervous);
        ShownCrowd = 0;
        ShownOrder = round.TurnOrder.ToArray();
        roundShown = 0;
        MoveCursor = 0;
        Mover = -1;
        Showing = -1;
        ResultStage = 0;
        Enter(AppealPhase.Lineup);
        Line = Text.Of("appeal.welcome", ("rank", Text.Of($"appeal.rank.{round.Rank}")), ("category", Text.Of($"contest.category.{round.Category}")));
        AudioManager.PlaySound("menu_open");
    }

    /// <summary>Puts the screen away without anything more (the game is leaving it).</summary>
    public void Close()
    {
        IsActive = false;
        IsShown = false;
        Phase = AppealPhase.Done;
    }

    private void Enter(AppealPhase phase)
    {
        Phase = phase;
        PhaseTime = 0f;
        wait = 0f;
    }

    public string NameOf(int contestant) => Round!.Appealers[contestant].Pokemon.DisplayName;

    /// <summary>The place a contestant came: "1st".</summary>
    public static string PlaceName(int place) => Text.Of($"appeal.place.{place}");

    private static string Fear(ContestStat quality) => Text.Of($"appeal.fear.{quality}");

    private static string CategoryName(ContestStat quality) => Text.Of($"contest.category.{quality}");

    // ---------------------------------------------------------------- the buttons

    /// <summary>The confirm button: on past a line, a move chosen.</summary>
    public void PressConfirm()
    {
        if (!IsActive || Round == null) return;
        switch (Phase)
        {
            case AppealPhase.Lineup: NextInLineup(); break;
            case AppealPhase.Choose: ChooseMove(); break;
            case AppealPhase.Turn: NextLine(); break;
            case AppealPhase.RoundEnd: AfterRound(); break;
            case AppealPhase.Results: NextResult(); break;
            case AppealPhase.Award: NextAwardLine(); break;
        }
    }

    /// <summary>The cancel button: as confirm, except where there is a choice to make.</summary>
    public void PressCancel()
    {
        if (!IsActive) return;
        if (Phase != AppealPhase.Choose) PressConfirm();
    }

    /// <summary>Moves the cursor over the player's moves.</summary>
    public void Move(int dx, int dy)
    {
        if (!IsActive || Phase != AppealPhase.Choose || Round == null || (dx == 0 && dy == 0)) return;
        int count = Round.Appealers[Player].Moves.Count;
        MoveCursor = UiNav.Wrap(MoveCursor, dy != 0 ? dy : dx, count);
        AudioManager.PlaySound("cursor");
    }

    // ---------------------------------------------------------------- the line-up

    // The contestants are announced one by one in the order of their first turn, then the first round is asked for
    private void NextInLineup()
    {
        var round = Round!;
        AudioManager.PlaySound("select");
        Showing++;
        if (Showing >= AppealRound.Contestants)
        {
            Showing = -1;
            BeginRound();
            return;
        }
        int who = round.ContestantAtTurn(Showing);
        var a = round.Appealers[who];
        Line = Text.Of("appeal.entry", ("n", Showing + 1), ("trainer", a.Trainer), ("name", a.Pokemon.DisplayName), ("points", a.Round1Points));
        PhaseTime = 0f;
        wait = 0f;
    }

    // ---------------------------------------------------------------- choosing

    private void BeginRound()
    {
        var round = Round!;
        if (round.IsOver)
        {
            BeginResults();
            return;
        }
        for (int i = 0; i < AppealRound.Contestants; i++)
        {
            totalsAtRoundStart[i] = round.Status[i].PointTotal;
            ShownTotal[i] = round.Status[i].PointTotal;
            ShownAppeal[i] = 0;
            ShownStars[i] = round.Status[i].Condition / 10;
            ShownEye[i] = round.Status[i].HasJudgesAttention;
            ShownNervous[i] = false;
        }
        ShownCrowd = round.ApplauseLevel;
        ShownOrder = round.TurnOrder.ToArray();
        roundShown = round.AppealNumber;
        Mover = -1;
        if (round.Status[Player].TurnDisabled)
        {
            // A Pokémon that can't take the round has nothing to choose: it is told so and the round plays
            round.ChooseMoves(0);
            lines.Clear();
            lines.Enqueue((Text.Of("appeal.noChoice", ("name", NameOf(Player))), null));
            Enter(AppealPhase.Turn);
            QueueTurn();
            return;
        }
        MoveCursor = 0;
        Enter(AppealPhase.Choose);
        Line = Text.Of("appeal.round", ("n", RoundNumber), ("of", AppealRound.Appeals), ("name", NameOf(Player)));
    }

    private void ChooseMove()
    {
        var round = Round!;
        if (MoveCursor >= round.Appealers[Player].Moves.Count) return;
        AudioManager.PlaySound("select");
        round.ChooseMoves(MoveCursor);
        Enter(AppealPhase.Turn);
        QueueTurn();
    }

    // ---------------------------------------------------------------- the turns

    private void QueueTurn()
    {
        var round = Round!;
        int turn = round.TurnNumber;
        int who = round.ContestantAtTurn(turn);
        Mover = who;
        MoveShown = round.Status[who].CurrMove?.Name ?? "";
        var events = round.PlayTurn();
        foreach (var e in events)
        {
            foreach (var text in Say(e)) lines.Enqueue((text, e));
            if (e.Kind is AppealEventKind.TurnOrder or AppealEventKind.ConditionUp or AppealEventKind.ConditionDown or AppealEventKind.Jammed)
                lines.Enqueue(("", e));
        }
        NextLine();
    }

    // The line(s) of an event; an event with only a picture says nothing
    private IEnumerable<string> Say(AppealEvent e)
    {
        string name = NameOf(e.Who);
        switch (e.Kind)
        {
            case AppealEventKind.Watching:
                yield return Text.Of("appeal.watching", ("name", name));
                break;
            case AppealEventKind.TooNervous:
                yield return Text.Of("appeal.tooNervous", ("name", name));
                break;
            case AppealEventKind.Appealed:
                yield return Text.Of("appeal.appealed", ("name", name), ("move", LocalNames.Move(e.Move)));
                break;
            case AppealEventKind.Line when e.Line is { } line:
                yield return Text.Of($"appeal.line.{line}", ("name", name), ("move", LocalNames.Move(e.Move)),
                    ("fear", Fear(Round!.Status[Mover].MoveCategory)));
                break;
            case AppealEventKind.SkipsNext:
                yield return Text.Of("appeal.skipsNext", ("name", name));
                break;
            case AppealEventKind.ComboStarted:
                yield return Text.Of("appeal.comboStarted", ("name", name));
                break;
            case AppealEventKind.ComboDone:
                yield return Text.Of("appeal.comboDone", ("name", name));
                break;
            case AppealEventKind.Repeated:
                yield return Text.Of("appeal.repeated", ("name", name));
                break;
            case AppealEventKind.CrowdUp:
                yield return Text.Of(e.Other > AppealRound.ApplauseSize - 1 ? "appeal.crowdOverflow" : "appeal.crowdUp", ("name", name),
                    ("quality", CategoryName(Round!.Status[e.Who].MoveCategory)));
                break;
            case AppealEventKind.CrowdDown:
                yield return Text.Of("appeal.crowdDown", ("name", name), ("quality", CategoryName(Round!.Status[e.Who].MoveCategory)));
                break;
            case AppealEventKind.CrowdWatches:
                yield return Text.Of("appeal.crowdWatches", ("name", name), ("freezer", NameOf(e.Other)), ("move", LocalNames.Move(e.Move)));
                break;
        }
    }

    // What an event does to the stage's numbers, as its line is shown
    private void Apply(AppealEvent e)
    {
        var round = Round!;
        switch (e.Kind)
        {
            case AppealEventKind.Appealed:
                ShownAppeal[e.Who] = e.Amount;
                break;
            case AppealEventKind.TooNervous:
                ShownNervous[e.Who] = true;
                break;
            case AppealEventKind.Jammed:
                ShownAppeal[e.Who] -= e.Amount;
                break;
            case AppealEventKind.ComboDone:
                ShownAppeal[e.Who] += e.Amount;
                ShownEye[e.Who] = false;
                break;
            case AppealEventKind.ComboStarted:
                ShownEye[e.Who] = true;
                break;
            case AppealEventKind.Repeated:
                ShownAppeal[e.Who] -= e.Amount;
                break;
            case AppealEventKind.ConditionUp:
                ShownStars[e.Who] = e.Amount / 10;
                break;
            case AppealEventKind.ConditionDown:
                ShownStars[e.Who] = 0;
                break;
            case AppealEventKind.CrowdUp:
                ShownAppeal[e.Who] += e.Amount;
                ShownCrowd = e.Other > AppealRound.ApplauseSize - 1 ? 0 : e.Other;
                break;
            case AppealEventKind.CrowdDown:
                ShownCrowd = e.Other;
                break;
            case AppealEventKind.Line:
                if (e.Line == AppealLine.JudgeLookAway) ShownEye[e.Who] = false;
                break;
        }
    }

    // The next line of the turn; with none left, the turn is over
    private void NextLine()
    {
        while (lines.Count > 0)
        {
            var (text, e) = lines.Dequeue();
            if (e != null) Apply(e);
            if (text.Length == 0) continue;
            Line = text;
            wait = 0f;
            return;
        }
        EndTurn();
    }

    private void EndTurn()
    {
        var round = Round!;
        if (round.TurnNumber == 0)
        {
            // The round was ranked as the fourth turn ended
            ShownOrder = round.TurnOrder.ToArray();
            Enter(AppealPhase.RoundEnd);
            var gained = Enumerable.Range(0, AppealRound.Contestants).Select(i => round.Status[i].PointTotal - totalsAtRoundStart[i]).ToArray();
            int best = Enumerable.Range(0, AppealRound.Contestants).OrderByDescending(i => gained[i]).First();
            Line = Text.Of("appeal.roundEnd", ("n", roundShown + 1), ("name", NameOf(best)), ("hearts", AppealRules.HeartsOf(gained[best])));
            AudioManager.PlaySound("select");
            return;
        }
        QueueTurn();
    }

    private void AfterRound()
    {
        AudioManager.PlaySound("select");
        for (int i = 0; i < AppealRound.Contestants; i++) ShownTotal[i] = Round!.Status[i].PointTotal;
        BeginRound();
    }

    // ---------------------------------------------------------------- the results

    private void BeginResults()
    {
        ResultStage = 0;
        Enter(AppealPhase.Results);
        Line = Text.Of("appeal.allOut");
    }

    private void NextResult()
    {
        var round = Round!;
        AudioManager.PlaySound("select");
        if (ResultStage < 3)
        {
            ResultStage++;
            PhaseTime = 0f;
            Line = ResultStage switch
            {
                1 => Text.Of("appeal.resultsAppeals"),
                2 => Text.Of("appeal.resultsTotal"),
                _ => Text.Of("appeal.winner", ("name", NameOf(Array.IndexOf(round.FinalStandings, 0))), ("trainer", round.Appealers[Array.IndexOf(round.FinalStandings, 0)].Trainer))
            };
            if (ResultStage == 3) Conclude();
            return;
        }
        BeginAward();
    }

    // The winner's prize, once, as the winner is named
    private void Conclude()
    {
        var round = Round!;
        var pokemon = round.Appealers[Player].Pokemon;
        Outcome = Practice
            ? new AppealOutcome(round.Category, round.Rank, round.FinalStandings[Player], round.TotalPoints[Player], round.FinalStandings[Player] == 0, null, false, false, round.Appealers[Player].Round1Points)
            : HoennContestRules.Conclude(round, pokemon);
    }

    private void BeginAward()
    {
        var o = Outcome!;
        var round = Round!;
        string name = NameOf(Player);
        awardLines.Clear();
        if (!o.Won) awardLines.Enqueue(Text.Of("appeal.placed", ("name", name), ("place", Text.Of($"appeal.place.{o.Placement}"))));
        else if (Practice) awardLines.Enqueue(Text.Of("appeal.practiceWon", ("name", name)));
        else
        {
            if (o.Ribbon != null) awardLines.Enqueue(Text.Of("appeal.ribbon", ("name", name), ("ribbon", RibbonDatabase.Name(o.Ribbon))));
            else if (o.GaveLuxuryBall) awardLines.Enqueue(Text.Of("appeal.luxuryBall", ("name", name)));
            else awardLines.Enqueue(Text.Of("appeal.wonAgain", ("name", name)));
            if (o.PaintingOffered) awardLines.Enqueue(Text.Of("appeal.paintingComing", ("name", name)));
        }
        if (awardLines.Count == 0)
        {
            End();
            return;
        }
        Enter(AppealPhase.Award);
        Line = awardLines.Dequeue();
        if (o.Won) AudioManager.PlaySound("stat_up");
    }

    private void NextAwardLine()
    {
        AudioManager.PlaySound("select");
        if (awardLines.Count > 0)
        {
            Line = awardLines.Dequeue();
            PhaseTime = 0f;
            wait = 0f;
            return;
        }
        End();
    }

    private void End()
    {
        IsActive = false;
        Enter(AppealPhase.Done);
    }

    // ---------------------------------------------------------------- the clock, the keys and the picture

    /// <summary>Time passes; a line that has been up long enough goes on by itself.</summary>
    public void Advance(float dt)
    {
        if (!IsActive) return;
        PhaseTime += dt;
        wait += dt;
        if (Phase is AppealPhase.Turn && wait >= LineWait) NextLine();
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Advance(dt);
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (dx != 0 || dy != 0) Move(dx, dy);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) PressConfirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) PressCancel();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsShown || Round == null) return;
        ModernUi.DrawAppeals(screenWidth, screenHeight, this);
    }
}
