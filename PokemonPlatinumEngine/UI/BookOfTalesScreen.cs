using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Book of Tales (plan 46 · CP11; style guide, "Menu screens", "The Book of Tales"): the tales learned, one to a page
/// in the order the table has them, each with its story and what it does when told; left and right turn the page, as
/// the Journal's do. Opened from the bag or the item button. Its logic takes no input (<see cref="Turn"/>, <see cref="Close"/>),
/// so tests and the harness drive it, and it touches no GPU, so a headless game may open it.
/// </summary>
public class BookOfTalesScreen
{
    private const float AppearTime = 0.3f;
    private float openAge;

    public bool IsActive { get; private set; }

    /// <summary>The tales learned, by id, as the Book was opened.</summary>
    public IReadOnlyList<string> Tales { get; private set; } = Array.Empty<string>();

    /// <summary>The page open, from the first tale (0).</summary>
    public int Page { get; private set; }

    /// <summary>The tale on the open page, or null for a Book with none.</summary>
    public string? Current => Page < Tales.Count ? Tales[Page] : null;

    /// <summary>How far the Book has come up, 0..1.</summary>
    public float Appear => Math.Clamp(openAge / AppearTime, 0f, 1f);

    public void Open(IReadOnlyList<string> tales)
    {
        IsActive = true;
        Tales = tales;
        Page = 0;
        openAge = 0f;
        AudioManager.PlaySound("page");
    }

    public void Close() => IsActive = false;

    /// <summary>To the next page (+1) or the one before (−1), as far as the tales learned go.</summary>
    public void Turn(int way)
    {
        int next = Math.Clamp(Page + Math.Sign(way), 0, Math.Max(0, Tales.Count - 1));
        if (next == Page) return;
        Page = next;
        AudioManager.PlaySound("page");
    }

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        if (InputManager.IsActionPressed(GameAction.Left)) Turn(-1);
        else if (InputManager.IsActionPressed(GameAction.Right)) Turn(1);
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Confirm))
        {
            Close();
            AudioManager.PlaySound("cancel");
        }
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawBookOfTales(screenWidth, screenHeight, this, Appear);
    }
}
