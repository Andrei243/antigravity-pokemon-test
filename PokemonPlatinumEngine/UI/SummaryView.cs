using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The pages of a Pokémon's summary (plan 08 · P3). The numbers are the ones the party screen has always used for the
/// pages it had (the Pokémon's own, the condition, the ribbons), so what was saved or written down in them still means
/// the same; the pages added since stand after them. <see cref="Info"/> is the Pokémon, its held item, its marks and
/// (until plan 08 · P4 gives them pages of their own) its moves; <see cref="Skills"/> its stats and ability.
/// </summary>
public enum SummaryPage { Info = 0, Condition = 1, Ribbons = 2, Memo = 3, Skills = 4 }

/// <summary>
/// What a summary shows and where its cursors are, with nothing drawn and no key read (plan 08 · P3): the page, the pages
/// the Pokémon has to turn through, the ribbons' cursor and the marks' cursor. <see cref="PartyScreen"/> owns one and the
/// PC and the battle's switch panel open the same view later (plan 08 · P4). The painter is <c>ModernUi.DrawSummary</c>.
/// </summary>
public sealed class SummaryView
{
    /// <summary>How many ribbons a row of the ribbons' page shows (the original shows six by three).</summary>
    public const int RibbonColumns = 6, RibbonRows = 3;

    public SummaryPage Page { get; private set; }

    /// <summary>Whether the contest condition has a page (once the Contest Hall has been visited).</summary>
    public bool HasCondition { get; set; }

    /// <summary>The ribbon under the cursor on the ribbons' page; -1 while the page only shows them.</summary>
    public int RibbonCursor { get; private set; } = -1;

    /// <summary>The mark under the cursor while the marks are being set (0 to 5, in <see cref="Markings"/>'s order); -1 otherwise.</summary>
    public int MarkCursor { get; private set; } = -1;

    /// <summary>The pages turned through, in order: the condition's only once the Contest Hall has been visited.</summary>
    public IReadOnlyList<SummaryPage> Pages => HasCondition
        ? new[] { SummaryPage.Info, SummaryPage.Memo, SummaryPage.Skills, SummaryPage.Condition, SummaryPage.Ribbons }
        : new[] { SummaryPage.Info, SummaryPage.Memo, SummaryPage.Skills, SummaryPage.Ribbons };

    /// <summary>Starts again on the first page with the cursors put away.</summary>
    public void Open()
    {
        Page = SummaryPage.Info;
        RibbonCursor = -1;
        MarkCursor = -1;
    }

    /// <summary>Puts the cursors away, as moving to another Pokémon does.</summary>
    public void PutCursorsAway()
    {
        RibbonCursor = -1;
        MarkCursor = -1;
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

    /// <summary>The confirm button on the first page: the marks' cursor comes up on the first mark. False on another page or for an Egg.</summary>
    public bool EditMarks(Pokemon shown)
    {
        if (Page != SummaryPage.Info || shown.IsEgg) return false;
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

    /// <summary>Whether a mark is set on the Pokémon (0 to 5).</summary>
    public static bool HasMark(Pokemon shown, int mark) => (shown.Marks & (1 << mark)) != 0;
}
