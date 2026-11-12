using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The gift box at a Gift Runner's (plan 40 · GE7; style guide, "Menu screens", "The gift box"): three tabs, WAITING (the
/// gifts due, one chosen to be handed over), CARDS (the album of gifts taken) and TELL WORDS (two words told to the runner).
/// Its steps take no input of their own, so tests and the harness drive it; what was chosen is <see cref="Outcome"/>,
/// which the engine hands the script as <see cref="IScriptHost.Answer"/>.
/// </summary>
public sealed class GiftBoxScreen
{
    private const float AppearTime = 0.35f;

    /// <summary>The cards on a row and the rows shown at once.</summary>
    public const int Columns = 3, VisibleRows = 2;

    public enum GiftTab { Waiting, Cards, Words }

    public bool IsActive { get; private set; }
    public GiftTab Tab { get; private set; }
    public int Cursor { get; private set; }
    public float Age { get; private set; }

    /// <summary>The gifts due when the box was opened.</summary>
    public IReadOnlyList<Gift> Due { get; private set; } = Array.Empty<Gift>();

    /// <summary>The gifts taken, each with its day, the latest first.</summary>
    public IReadOnlyList<(Gift Gift, DateTime Day)> Album { get; private set; } = Array.Empty<(Gift, DateTime)>();

    /// <summary>What the box was left with: a gift due (from 1), <see cref="GiftBoxAnswers.Words"/>, or 0 backed out of.</summary>
    public int Outcome { get; private set; }

    /// <summary>The first row of cards shown.</summary>
    public int Top { get; private set; }

    public int Count => Tab switch { GiftTab.Waiting => Due.Count, GiftTab.Cards => Album.Count, _ => 1 };

    public void Open(GiftBox box, GiftFacts facts)
    {
        Due = box.Due(facts);
        Album = box.Album();
        Tab = GiftTab.Waiting;
        Cursor = 0;
        Top = 0;
        Outcome = 0;
        Age = 0f;
        IsActive = true;
        AudioManager.PlaySound("menu_open");
    }

    public void Close()
    {
        IsActive = false;
        AudioManager.PlaySound("menu_close");
    }

    /// <summary>The tab to the left or the right, wrapping round.</summary>
    public void ChangeTab(int dx)
    {
        if (dx == 0) return;
        Tab = (GiftTab)UiNav.Wrap((int)Tab, Math.Sign(dx), 3);
        Cursor = 0;
        Top = 0;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The cursor over the cards, a grid of <see cref="Columns"/>.</summary>
    public void Move(int dx, int dy)
    {
        if (Count <= 1 || dx == 0 && dy == 0) return;
        int next = UiNav.Grid(Cursor, Count, Columns, dx, dy);
        if (next == Cursor) return;
        Cursor = next;
        int row = Cursor / Columns;
        if (row < Top) Top = row;
        if (row >= Top + VisibleRows) Top = row - VisibleRows + 1;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>A gift waiting chosen, or TELL WORDS: the box closes with it. Nothing on the album.</summary>
    public void Confirm()
    {
        if (Tab == GiftTab.Words)
        {
            Outcome = GiftBoxAnswers.Words;
            AudioManager.PlaySound("select");
            Close();
            return;
        }
        if (Tab != GiftTab.Waiting || Due.Count == 0) return;
        Outcome = Cursor + 1;
        AudioManager.PlaySound("select");
        Close();
    }

    public void Cancel()
    {
        Outcome = 0;
        Close();
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.PageLeft)) ChangeTab(-1);
        else if (InputManager.IsActionPressed(GameAction.PageRight)) ChangeTab(1);
        else Move(InputManager.Axis(GameAction.Left, GameAction.Right), InputManager.Axis(GameAction.Up, GameAction.Down));
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawGiftBox(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
