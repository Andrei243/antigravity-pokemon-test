using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Journal's pages (plan 06 · R12; the original's <c>journal_display</c>): the newest first, left and right to
/// turn to an older or a newer day, never past the last page written. Opened from the bag, or by itself as the game
/// is continued after two days or more. Its logic takes no input (<see cref="Turn"/>), so tests and the harness drive it.
/// </summary>
public class JournalScreen
{
    private const float AppearTime = 0.3f;
    private float openAge;

    public bool IsActive { get; private set; }

    /// <summary>The page open, counted from the newest (0).</summary>
    public int Page { get; private set; }

    public void Open()
    {
        IsActive = true;
        Page = 0;
        openAge = 0f;
        AudioManager.PlaySound("page");
    }

    public void Close() => IsActive = false;

    /// <summary>To an older page (+1) or a newer one (−1), as far as the pages written go.</summary>
    public void Turn(int older, Journal journal)
    {
        int next = Math.Clamp(Page + Math.Sign(older), 0, Math.Max(0, journal.All.Count - 1));
        if (next == Page) return;
        Page = next;
        AudioManager.PlaySound("page");
    }

    public void Update(Journal journal, float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        if (InputManager.IsActionPressed(GameAction.Left)) Turn(1, journal);
        else if (InputManager.IsActionPressed(GameAction.Right)) Turn(-1, journal);
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Confirm))
        {
            Close();
            AudioManager.PlaySound("cancel");
        }
    }

    public void Draw(int screenWidth, int screenHeight, Journal journal)
    {
        if (!IsActive) return;
        ModernUi.DrawJournal(screenWidth, screenHeight, this, journal, Math.Clamp(openAge / AppearTime, 0f, 1f));
    }
}
