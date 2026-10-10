using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Fashion Case looked into (plan 06 · R17b): its accessories as a grid with how many of each, its backdrops one
/// to a row with the one under the cursor shown large, and the contests entered and won; and, from the Contest Hall's
/// wall, the photograph of a contest's winner (<see cref="OpenPhoto"/>). The original's bag only says how many of
/// each the case holds; the viewer is ours. Its steps take no input of their own, so tests and the harness drive it.
/// </summary>
public sealed class FashionCaseScreen
{
    private const float AppearTime = 0.35f;

    /// <summary>The accessories' grid is so many wide; the backdrops' list shows so many rows.</summary>
    public const int Columns = 8, VisibleRows = 8;

    public static readonly string[] TabNames = { "ACCESSORIES", "BACKDROPS" };

    public bool IsActive { get; private set; }

    /// <summary>0 the accessories, 1 the backdrops.</summary>
    public int Tab { get; private set; }
    public int Cursor { get; private set; }
    public int First { get; private set; }

    /// <summary>The photograph shown instead of the case, when the screen was opened on one.</summary>
    public ContestPhoto? Photo { get; private set; }

    public float Age { get; private set; }

    private FashionCase fashionCase = new();
    private ContestRecords records = new();

    public FashionCase Case => fashionCase;
    public ContestRecords Records => records;

    /// <summary>The accessories in the case with how many of each, in the original's order.</summary>
    public IReadOnlyList<(int Accessory, int Count)> Accessories { get; private set; } = Array.Empty<(int, int)>();

    public void Open(FashionCase from, ContestRecords contests)
    {
        fashionCase = from;
        records = contests;
        Accessories = from.Held.ToList();
        Photo = null;
        IsActive = true;
        Tab = 0;
        Cursor = First = 0;
        Age = 0f;
        AudioManager.PlaySound("menu_open");
    }

    /// <summary>Opens on the photograph in a contest's frame.</summary>
    public void OpenPhoto(ContestPhoto photo)
    {
        Photo = photo;
        IsActive = true;
        Age = 0f;
        AudioManager.PlaySound("select");
    }

    public void Close()
    {
        IsActive = false;
        Photo = null;
        AudioManager.PlaySound("menu_close");
    }

    /// <summary>How many things the open tab lists.</summary>
    public int Count => Tab == 0 ? Accessories.Count : fashionCase.Backdrops.Count;

    /// <summary>Left and right over the backdrops, or the Run button anywhere, change the tab.</summary>
    public void MoveTab(int step)
    {
        if (Photo != null || step == 0) return;
        Tab = UiNav.Wrap(Tab, step, TabNames.Length);
        Cursor = First = 0;
        AudioManager.PlaySound("page");
    }

    /// <summary>The arrows: over the accessories' grid, or up and down the backdrops.</summary>
    public void Move(int dx, int dy)
    {
        if (Photo != null || Count == 0 || (dx == 0 && dy == 0)) return;
        int next = Tab == 0 ? UiNav.Grid(Cursor, Count, Columns, dx, dy) : UiNav.Wrap(Cursor, Math.Sign(dy), Count);
        if (next == Cursor) return;
        Cursor = next;
        if (Tab == 1) First = UiNav.Window(First, Cursor, Count, VisibleRows);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Either button closes a photograph; cancel closes the case.</summary>
    public void Confirm()
    {
        if (Photo != null) Close();
    }

    public void Cancel() => Close();

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Item) || (Tab == 1 && dx != 0)) MoveTab(dx != 0 ? dx : 1);
        else if (dx != 0 || dy != 0) Move(dx, dy);
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        float appear = Math.Clamp(Age / AppearTime, 0f, 1f);
        if (Photo != null) ModernUi.DrawContestPhoto(screenWidth, screenHeight, Photo, appear);
        else ModernUi.DrawFashionCase(screenWidth, screenHeight, this, appear);
    }
}
