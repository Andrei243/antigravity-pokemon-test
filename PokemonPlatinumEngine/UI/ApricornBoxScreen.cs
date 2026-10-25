using System;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Apricorn Box looked into (plan 20 · J6): the seven colours one to a row with how many of each the Box holds,
/// and for the one under the cursor the ball Kurt makes of it and what he is making now. HeartGold's Box only says how
/// many of each it holds; the list is ours. Its steps take no input of their own, so tests and the harness drive it.
/// </summary>
public sealed class ApricornBoxScreen
{
    private const float AppearTime = 0.35f;

    public bool IsActive { get; private set; }

    /// <summary>The colour under the cursor, 0 to 6.</summary>
    public int Cursor { get; private set; }

    public float Age { get; private set; }

    private ApricornBox box = new();

    public ApricornBox Box => box;

    public static Apricorn[] Colours => Enum.GetValues<Apricorn>();

    public Apricorn Selected => Colours[Cursor];

    public void Open(ApricornBox from)
    {
        box = from;
        IsActive = true;
        Cursor = Math.Max(0, Array.FindIndex(Colours, c => from.Count(c) > 0));
        Age = 0f;
        AudioManager.PlaySound("menu_open");
    }

    public void Close()
    {
        IsActive = false;
        AudioManager.PlaySound("menu_close");
    }

    /// <summary>Up and down the colours.</summary>
    public void Move(int dy)
    {
        if (dy == 0) return;
        Cursor = UiNav.Wrap(Cursor, Math.Sign(dy), Colours.Length);
        AudioManager.PlaySound("cursor");
    }

    public void Cancel() => Close();

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        int dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Confirm)) Cancel();
        else if (dy != 0) Move(dy);
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawApricornBox(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
