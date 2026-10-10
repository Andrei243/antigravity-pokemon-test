using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// A room decorated on screen (plan 06 · R16a; the shared seam plan 21 · H10's Secret Bases use too): the room seen
/// from above with its pieces, a cursor or the piece in hand moved over it by the arrows, and the store's list beside
/// it. The rules are <see cref="DecoratingSession"/>'s; this is its clock, its keys and its picture. It is given a
/// room, a store and a catalogue, and a way to colour a kind of piece, and knows nothing of whose room it is.
/// </summary>
public sealed class DecorationScreen
{
    public const int VisibleRows = 8;

    public bool IsActive { get; private set; }
    public DecoratingSession Session { get; private set; } = null!;
    public string Title { get; private set; } = "";
    public float Age { get; private set; }

    /// <summary>The first row of the store's list in view.</summary>
    public int First { get; private set; }

    /// <summary>How a kind of piece is drawn on the plan: its colour, and a letter or two to tell it by.</summary>
    public Func<RoomPiece, (Color Colour, string Mark)> Look { get; private set; } = _ => (new Color(150, 160, 190, 255), "");

    public void Open(DecoratingSession session, string title, Func<RoomPiece, (Color Colour, string Mark)> look)
    {
        Session = session;
        Title = title;
        Look = look;
        Age = 0f;
        First = 0;
        IsActive = true;
        AudioManager.PlaySound("select");
    }

    public void Close() => IsActive = false;

    public void Move(int dx, int dy)
    {
        var before = (Session.Step, Session.MenuCursor, Session.StoreCursor, Session.X, Session.Y);
        Session.Move(dx, dy);
        First = UiNav.Window(First, Session.StoreCursor, Session.Store.Count, VisibleRows);
        if (before != (Session.Step, Session.MenuCursor, Session.StoreCursor, Session.X, Session.Y)) AudioManager.PlaySound("cursor");
    }

    public void PressConfirm()
    {
        Session.Confirm();
        AudioManager.PlaySound(Session.Refused ? "error" : "select");
        if (Session.Step == DecoratingStep.Done) IsActive = false;
    }

    public void PressCancel()
    {
        Session.Cancel();
        AudioManager.PlaySound("cancel");
        if (Session.Step == DecoratingStep.Done) IsActive = false;
    }

    private readonly HeldKey heldX = new(), heldY = new();

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        int dx = heldX.Advance(dt, InputManager.Axis(GameAction.Left, GameAction.Right), InputManager.Axis(GameAction.Left, GameAction.Right, held: true));
        int dy = heldY.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down), InputManager.Axis(GameAction.Up, GameAction.Down, held: true));
        if (dx != 0 || dy != 0) Move(Math.Sign(dx), dx != 0 ? 0 : Math.Sign(dy));
        else if (InputManager.IsActionPressed(GameAction.Cancel)) PressCancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) PressConfirm();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (IsActive) ModernUi.DrawDecoration(screenWidth, screenHeight, this);
    }
}
