using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Bug-Catching Contest's results on screen (plan 15 · E5; style guide, "Menu screens", "The Bug-Catching
/// Contest's results"): the six entrants with what they caught and their scores, told third, second and first as the
/// original's announcer tells them, the winner's catch on a disc, the player's own row marked. A press tells the next
/// place, and once all are told closes the board. Its logic takes no input (<see cref="Open"/>, <see cref="Confirm"/>,
/// <see cref="Advance"/>), so tests and the harness drive it.
/// </summary>
public sealed class BugContestResultsScreen
{
    /// <summary>How many places are told one by one before the board is whole (third, second, first).</summary>
    public const int Told = 3;

    private const float AppearTime = 0.3f;

    public bool IsActive { get; private set; }
    public BugContest? Contest { get; private set; }

    /// <summary>How many of the three are told: 0 none yet, 3 the board whole.</summary>
    public int Revealed { get; private set; }

    /// <summary>Seconds since the board opened, and since the last place was told.</summary>
    public float Age { get; private set; }
    public float RevealAge { get; private set; }

    public void Open(BugContest contest)
    {
        Contest = contest;
        IsActive = true;
        Revealed = 0;
        Age = 0f;
        RevealAge = 0f;
    }

    /// <summary>Whether the place on the board (0 first) is told yet: those below third are told with the first.</summary>
    public bool Shows(int place) => place switch
    {
        0 => Revealed >= 3,
        1 => Revealed >= 2,
        2 => Revealed >= 1,
        _ => Revealed >= 3
    };

    public void Confirm()
    {
        if (!IsActive) return;
        if (Revealed < Told)
        {
            Revealed++;
            RevealAge = 0f;
            AudioManager.PlaySound(Revealed == Told ? "levelup" : "select");
            return;
        }
        AudioManager.PlaySound("select");
        IsActive = false;
    }

    public void Advance(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        RevealAge += dt;
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Advance(dt);
        if (InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Cancel)) Confirm();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive || Contest == null) return;
        ModernUi.DrawBugContestResults(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
