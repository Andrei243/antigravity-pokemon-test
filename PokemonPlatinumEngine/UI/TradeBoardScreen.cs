using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The trade board at a link desk (plan 40 · GE8; style guide, "Menu screens", "The trade board"): the region's three
/// requests of the day side by side and the blind trade's row under them. Its steps take no input of their own, so tests
/// and the harness drive it; <see cref="Outcome"/> is the request chosen (1 to 3), 4 for a blind trade, or 0, which the
/// engine hands the desk's script.
/// </summary>
public sealed class TradeBoardScreen
{
    private const float AppearTime = 0.35f;

    /// <summary>The place of the blind trade's row among the choices, after the requests.</summary>
    public int BlindRow => Requests.Count;

    public bool IsActive { get; private set; }
    public int Cursor { get; private set; }
    public float Age { get; private set; }
    public string Region { get; private set; } = "";
    public IReadOnlyList<BoardRequest> Requests { get; private set; } = Array.Empty<BoardRequest>();
    public IReadOnlyList<bool> Met { get; private set; } = Array.Empty<bool>();
    public int BlindLeft { get; private set; }

    /// <summary>Whether the player has seen a species, so a request's wants are named; every one until the engine says.</summary>
    public Func<string, bool> Seen { get; set; } = _ => true;

    public int Outcome { get; private set; }

    public void Open(string region, IReadOnlyList<BoardRequest> requests, IReadOnlyList<bool> met, int blindLeft)
    {
        Region = region;
        Requests = requests;
        Met = met;
        BlindLeft = blindLeft;
        Cursor = 0;
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

    /// <summary>Left and right along the requests, down to the blind trade and up from it.</summary>
    public void Move(int dx, int dy)
    {
        int next = Cursor;
        if (dy > 0) next = BlindRow;
        else if (dy < 0 && Cursor == BlindRow) next = Math.Max(0, Math.Min(Requests.Count - 1, 1));
        else if (dx != 0 && Cursor < BlindRow && Requests.Count > 0) next = UiNav.Wrap(Cursor, Math.Sign(dx), Requests.Count);
        if (next == Cursor) return;
        Cursor = next;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>A request not met, or the blind trade with some left: the board closes with it.</summary>
    public void Confirm()
    {
        if (Cursor == BlindRow ? BlindLeft <= 0 : Cursor < Met.Count && Met[Cursor])
        {
            AudioManager.PlaySound("cancel");
            return;
        }
        Outcome = Cursor == BlindRow ? TradeBoard.RequestsADay + 1 : Cursor + 1;
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
        else Move(InputManager.Axis(GameAction.Left, GameAction.Right), InputManager.Axis(GameAction.Up, GameAction.Down));
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawTradeBoard(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
