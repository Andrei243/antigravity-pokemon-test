using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// A facility's records board (plan 06 · R18a; style guide, "The Battle Frontier's board"): the streaks of each way
/// its challenge is taken, the Battle Points held and the print won, until a button closes it. Its logic takes no
/// input (<see cref="Open"/>, <see cref="Close"/>), so tests and the harness drive it.
/// </summary>
public sealed class FrontierRecordsScreen
{
    private const float AppearTime = 0.3f;
    private float openAge;

    public bool IsActive { get; private set; }
    public FrontierBoard? Board { get; private set; }

    public void Open(FrontierBoard board)
    {
        Board = board;
        IsActive = true;
        openAge = 0f;
        AudioManager.PlaySound("select");
    }

    public void Close()
    {
        IsActive = false;
        AudioManager.PlaySound("cancel");
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        openAge += dt;
        if (InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu)) Close();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive || Board == null) return;
        ModernUi.DrawFrontierBoard(screenWidth, screenHeight, Board, Math.Clamp(openAge / AppearTime, 0f, 1f));
    }
}
