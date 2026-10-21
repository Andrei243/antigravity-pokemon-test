using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The pages of a Pokémon's summary (plan 08 · P3, P4). The numbers are the ones the party screen has always used for the
/// pages it had (the Pokémon's own, the condition, the ribbons), so what was saved or written down in them still means
/// the same; the pages added since stand after them. <see cref="Info"/> is the Pokémon, its held item and its marks;
/// <see cref="Skills"/> its stats and ability; <see cref="BattleMoves"/> its moves with the chosen one's text and
/// <see cref="ContestMoves"/> the same moves as the Acting Competition sees them.
/// </summary>
public enum SummaryPage { Info = 0, Condition = 1, Ribbons = 2, Memo = 3, Skills = 4, BattleMoves = 5, ContestMoves = 6 }

/// <summary>
/// What a summary shows and where its cursors are, with nothing drawn (plan 08 · P3, P4): the page, the pages the Pokémon
/// has to turn through, the cursors on the ribbons, the marks and the moves, and one frame of the keys that move them
/// (<see cref="Press"/>). <see cref="PartyScreen"/>, the PC and the battle's switch panel each own one. The painter is
/// <c>ModernUi.DrawSummary</c>.
/// </summary>
public sealed class SummaryView
{
    /// <summary>How many ribbons a row of the ribbons' page shows (the original shows six by three).</summary>
    public const int RibbonColumns = 6, RibbonRows = 3;

    public SummaryPage Page { get; private set; }

    /// <summary>Whether the contest condition has a page (once the Contest Hall has been visited).</summary>
    public bool HasCondition { get; set; }

    /// <summary>Whether the marks may be set from this view: the team's and the PC's may, a battle's may not.</summary>
    public bool CanMark { get; set; } = true;

    /// <summary>The ribbon under the cursor on the ribbons' page; -1 while the page only shows them.</summary>
    public int RibbonCursor { get; private set; } = -1;

    /// <summary>The mark under the cursor while the marks are being set (0 to 5, in <see cref="Models.Markings"/>'s order); -1 otherwise.</summary>
    public int MarkCursor { get; private set; } = -1;

    /// <summary>The move under the cursor on either move page; -1 while the pages only show them.</summary>
    public int MoveSelection { get; private set; } = -1;

    /// <summary>The pages turned through, in order: the condition's only once the Contest Hall has been visited.</summary>
    public IReadOnlyList<SummaryPage> Pages => HasCondition
        ? new[] { SummaryPage.Info, SummaryPage.Memo, SummaryPage.Skills, SummaryPage.BattleMoves, SummaryPage.ContestMoves, SummaryPage.Condition, SummaryPage.Ribbons }
        : new[] { SummaryPage.Info, SummaryPage.Memo, SummaryPage.Skills, SummaryPage.BattleMoves, SummaryPage.ContestMoves, SummaryPage.Ribbons };

    /// <summary>Whether the page open is one of the two that show the moves.</summary>
    public bool OnAMovePage => Page is SummaryPage.BattleMoves or SummaryPage.ContestMoves;

    /// <summary>Starts again on the first page with the cursors put away.</summary>
    public void Open()
    {
        Page = SummaryPage.Info;
        PutCursorsAway();
    }

    /// <summary>Puts the cursors away, as moving to another Pokémon does.</summary>
    public void PutCursorsAway()
    {
        RibbonCursor = -1;
        MarkCursor = -1;
        MoveSelection = -1;
    }

    /// <summary>Turns to the next page or the one before, round and round.</summary>
    public void Turn(int dx)
    {
        if (dx == 0) return;
        var pages = Pages;
        int at = Math.Max(0, pages.ToList().IndexOf(Page));
        Page = pages[UiNav.Wrap(at, Math.Sign(dx), pages.Count)];
        PutCursorsAway();
        AudioManager.PlaySound("page");
    }

    // ---------------------------------------------------------------- the ribbons

    /// <summary>The cursor comes up on the first ribbon; false when the Pokémon has none or another page is open.</summary>
    public bool SelectRibbons(Pokemon shown)
    {
        if (Page != SummaryPage.Ribbons || shown.IsEgg || shown.Ribbons.Count == 0) return false;
        RibbonCursor = 0;
        AudioManager.PlaySound("select");
        return true;
    }

    public void LeaveRibbons()
    {
        RibbonCursor = -1;
        AudioManager.PlaySound("cancel");
    }

    /// <summary>Moves the ribbons' cursor over their rows of <see cref="RibbonColumns"/>, round at the edges.</summary>
    public void MoveRibbon(int dx, int dy, int count)
    {
        if (RibbonCursor < 0 || count <= 0 || (dx == 0 && dy == 0)) return;
        int next = UiNav.Grid(Math.Min(RibbonCursor, count - 1), count, RibbonColumns, dx, dy);
        if (next == RibbonCursor) return;
        RibbonCursor = next;
        AudioManager.PlaySound("cursor");
    }

    // ---------------------------------------------------------------- the marks

    /// <summary>The confirm button on the first page: the marks' cursor comes up on the first mark. False on another page, for an Egg or where marks may not be set.</summary>
    public bool EditMarks(Pokemon shown)
    {
        if (Page != SummaryPage.Info || shown.IsEgg || !CanMark) return false;
        MarkCursor = 0;
        AudioManager.PlaySound("select");
        return true;
    }

    /// <summary>Moves the marks' cursor along the six, round at the ends.</summary>
    public void MoveMark(int dx)
    {
        if (MarkCursor < 0 || dx == 0) return;
        MarkCursor = UiNav.Wrap(MarkCursor, Math.Sign(dx), ModernUi.MarkCount);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Sets or clears the mark under the cursor on the Pokémon.</summary>
    public void ToggleMark(Pokemon shown)
    {
        if (MarkCursor < 0) return;
        shown.Marks ^= 1 << MarkCursor;
        AudioManager.PlaySound("select");
    }

    public void LeaveMarks()
    {
        MarkCursor = -1;
        AudioManager.PlaySound("cancel");
    }

    // ---------------------------------------------------------------- the moves

    /// <summary>The confirm button on a move page: the cursor comes up on the first move, whose text is then shown. False on another page or with no moves.</summary>
    public bool LookAtMoves(Pokemon shown)
    {
        if (!OnAMovePage || shown.IsEgg || shown.Moves.Count == 0) return false;
        MoveSelection = 0;
        AudioManager.PlaySound("select");
        return true;
    }

    /// <summary>Moves the cursor down the moves, round at the ends.</summary>
    public void MoveMove(int dy, int count)
    {
        if (MoveSelection < 0 || count <= 0 || dy == 0) return;
        int next = UiNav.Wrap(Math.Min(MoveSelection, count - 1), Math.Sign(dy), count);
        if (next == MoveSelection) return;
        MoveSelection = next;
        AudioManager.PlaySound("cursor");
    }

    public void LeaveMoves()
    {
        MoveSelection = -1;
        AudioManager.PlaySound("cancel");
    }

    // ---------------------------------------------------------------- the keys

    /// <summary>What a frame's keys did to the summary.</summary>
    /// <param name="Close">The summary is to be put away.</param>
    /// <param name="Step">Up or down (-1 or 1) with no cursor open: whoever owns the view goes to another Pokémon.</param>
    public readonly record struct Result(bool Close, int Step);

    /// <summary>
    /// One frame of the summary's keys, the same wherever it is opened: while a cursor is up (the ribbons, the marks,
    /// the moves) the arrows move it and the buttons set or leave it; otherwise the confirm button brings the page's
    /// cursor up where it has one and puts the summary away where it has not, cancel puts it away, left and right turn
    /// the page (an Egg has the one) and up and down are handed back as a <see cref="Result.Step"/>.
    /// </summary>
    public Result Press(Pokemon? shown, int dx, int dy)
    {
        bool cancel = InputManager.IsActionPressed(GameAction.Cancel), confirm = InputManager.IsActionPressed(GameAction.Confirm);
        if (RibbonCursor >= 0 && shown != null)
        {
            if (cancel || confirm) LeaveRibbons();
            else MoveRibbon(dx, dy, shown.Ribbons.Count);
            return default;
        }
        if (MarkCursor >= 0 && shown != null)
        {
            if (cancel) LeaveMarks();
            else if (confirm) ToggleMark(shown);
            else MoveMark(dx);
            return default;
        }
        if (MoveSelection >= 0 && shown != null)
        {
            if (cancel || confirm) LeaveMoves();
            else MoveMove(dy, shown.Moves.Count);
            return default;
        }
        if (confirm && shown != null && (SelectRibbons(shown) || EditMarks(shown) || LookAtMoves(shown))) return default;
        if (cancel || confirm)
        {
            AudioManager.PlaySound("cancel");
            return new Result(true, 0);
        }
        if (dx != 0 && shown is { IsEgg: false }) Turn(dx);
        else if (dy != 0) return new Result(false, dy);
        return default;
    }

    /// <summary>Whether a mark is set on the Pokémon (0 to 5).</summary>
    public static bool HasMark(Pokemon shown, int mark) => (shown.Marks & (1 << mark)) != 0;
}
