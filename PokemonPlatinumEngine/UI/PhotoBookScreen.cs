using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The photo book (plan 43 · LF4; style guide, "Menu screens", "The photo book"): pages of six of the player's
/// photographs, newest last, a tab for ALL and one for each region with a photograph; A opens one large with its place
/// and day, the item button there asks to throw it away. Its logic takes no input (<see cref="Move"/>,
/// <see cref="ChangeTab"/>, <see cref="Confirm"/>, <see cref="Cancel"/>, <see cref="AskThrow"/>); what it asks of the
/// game (the files of a photograph thrown away) comes back through <see cref="TakeThrown"/>. Pictures are the engine's
/// to load, for the page in view only.
/// </summary>
public sealed class PhotoBookScreen
{
    /// <summary>How many photographs a page holds: three across, two down.</summary>
    public const int PerPage = 6, Columns = 3;

    private PhotoBook book = new();
    private readonly List<string> tabs = new();
    private PhotoPrint? thrown;

    public bool IsActive { get; private set; }

    /// <summary>The tab open: 0 for ALL, then each region with a photograph.</summary>
    public int Tab { get; private set; }

    /// <summary>The regions' tabs after ALL.</summary>
    public IReadOnlyList<string> Tabs => tabs;

    /// <summary>The photograph under the cursor in the tab's list.</summary>
    public int Index { get; private set; }

    /// <summary>One opened large, and the throwing away's question over it.</summary>
    public bool Opened { get; private set; }
    public bool Asking { get; private set; }
    public int AskIndex { get; private set; }

    public float Age { get; private set; }

    /// <summary>The tab's photographs, in the book's order.</summary>
    public IReadOnlyList<PhotoPrint> Shown =>
        Tab == 0 ? book.Prints : book.Prints.Where(p => string.Equals(p.Region, tabs[Tab - 1], StringComparison.OrdinalIgnoreCase)).ToList();

    public int Page => Index / PerPage;
    public int Pages => Math.Max(1, (Shown.Count + PerPage - 1) / PerPage);

    /// <summary>The page's photographs.</summary>
    public IReadOnlyList<PhotoPrint> PageNow => Shown.Skip(Page * PerPage).Take(PerPage).ToList();

    public PhotoPrint? Selected => Index < Shown.Count ? Shown[Index] : null;

    public int Total => book.Count;

    public void Open(PhotoBook photos)
    {
        book = photos;
        tabs.Clear();
        tabs.AddRange(photos.Regions);
        Tab = 0;
        Index = Math.Max(0, photos.Count - 1);
        Opened = Asking = false;
        thrown = null;
        Age = 0f;
        IsActive = true;
        AudioManager.PlaySound("select");
    }

    public void Close() => IsActive = false;

    /// <summary>The arrows: a photograph left or right, a row up or down, held within the tab's list.</summary>
    public void Move(int dx, int dy)
    {
        if (Asking)
        {
            if (dx != 0 || dy != 0) { AskIndex = 1 - AskIndex; AudioManager.PlaySound("cursor"); }
            return;
        }
        int count = Shown.Count;
        if (count == 0 || (dx == 0 && dy == 0)) return;
        int to = Math.Clamp(Index + dx + dy * Columns, 0, count - 1);
        if (to == Index) return;
        Index = to;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The shoulders: the tab before or after (ALL, then the regions), wrapping.</summary>
    public void ChangeTab(int step)
    {
        if (step == 0 || Opened || tabs.Count == 0) return;
        Tab = UiNav.Wrap(Tab, Math.Sign(step), tabs.Count + 1);
        Index = 0;
        AudioManager.PlaySound("page");
    }

    /// <summary>The A button: a photograph opened large, or the question's answer.</summary>
    public void Confirm()
    {
        if (Asking)
        {
            Asking = false;
            if (AskIndex == 0 && Selected is { } print)
            {
                thrown = print;
                book.Remove(print);
                tabs.Clear();
                tabs.AddRange(book.Regions);
                if (Tab > tabs.Count) Tab = 0;
                Index = Math.Clamp(Index, 0, Math.Max(0, Shown.Count - 1));
                Opened = false;
                AudioManager.PlaySound("select");
            }
            else AudioManager.PlaySound("cancel");
            return;
        }
        if (Selected == null) return;
        Opened = true;
        AudioManager.PlaySound("select");
    }

    /// <summary>The item button on a photograph opened large: "throw it away?", NO first.</summary>
    public void AskThrow()
    {
        if (!Opened || Asking || Selected == null) return;
        Asking = true;
        AskIndex = 1;
        AudioManager.PlaySound("select");
    }

    /// <summary>The B button: out of the question, back from a photograph to its page, then out of the book.</summary>
    public void Cancel()
    {
        if (Asking) Asking = false;
        else if (Opened) Opened = false;
        else Close();
        AudioManager.PlaySound("cancel");
    }

    /// <summary>The photograph thrown away since last asked, once: the engine deletes its two files.</summary>
    public PhotoPrint? TakeThrown()
    {
        var t = thrown;
        thrown = null;
        return t;
    }

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        Age += dt;
        int dx = InputManager.IsActionPressed(GameAction.Left) ? -1 : InputManager.IsActionPressed(GameAction.Right) ? 1 : 0;
        int dy = InputManager.IsActionPressed(GameAction.Up) ? -1 : InputManager.IsActionPressed(GameAction.Down) ? 1 : 0;
        Move(dx, dy);
        if (InputManager.IsActionPressed(GameAction.PageLeft)) ChangeTab(-1);
        if (InputManager.IsActionPressed(GameAction.PageRight)) ChangeTab(1);
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Item)) AskThrow();
    }
}
