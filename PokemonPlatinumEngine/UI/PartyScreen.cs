using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Pokémon menu: the team as two columns of cards, reordering (Shift picks a Pokémon up, A puts it down),
/// and the summary of the chosen Pokémon, where up and down step through the team.
/// </summary>
public class PartyScreen
{
    private const float AppearTime = 0.45f, SummaryAppearTime = 0.25f;

    private float openAge, summaryAge;

    public int SelectedIndex { get; set; } = 0;
    public int? SwapSourceIndex { get; set; } = null;
    public bool ShowSummary { get; set; } = false;
    public bool IsActive { get; set; } = false;

    public void Open()
    {
        IsActive = true;
        SelectedIndex = 0;
        SwapSourceIndex = null;
        ShowSummary = false;
        openAge = 0f;
    }

    public void Close()
    {
        IsActive = false;
        SwapSourceIndex = null;
        ShowSummary = false;
    }

    /// <summary>Moves the cursor over the two-column list, or to the next Pokémon while the summary is open.</summary>
    public void MoveCursor(int dx, int dy, int count)
    {
        if (count <= 0 || (dx == 0 && dy == 0)) return;
        int next = ShowSummary
            ? ((SelectedIndex + (dy != 0 ? dy : dx)) % count + count) % count
            : UiNav.Grid(SelectedIndex, count, 2, dx, dy);
        if (next == SelectedIndex) return;
        SelectedIndex = next;
        AudioManager.PlaySound("cursor");
    }

    public void Update(Party party, float dt)
    {
        if (!IsActive) return;
        openAge += dt;
        summaryAge += dt;

        int dx = (InputManager.IsActionPressed(GameAction.Right) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
        int dy = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);

        if (ShowSummary)
        {
            if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Confirm))
            {
                ShowSummary = false;
                AudioManager.PlaySound("cancel");
            }
            else MoveCursor(dx, dy, party.Count);
            return;
        }

        if (dx != 0 || dy != 0)
        {
            MoveCursor(dx, dy, party.Count);
        }
        else if (InputManager.IsActionPressed(GameAction.Cancel))
        {
            if (SwapSourceIndex.HasValue) SwapSourceIndex = null;
            else Close();
            AudioManager.PlaySound("cancel");
        }
        else if (InputManager.IsActionPressed(GameAction.Confirm))
        {
            if (SwapSourceIndex.HasValue)
            {
                party.Swap(SwapSourceIndex.Value, SelectedIndex);
                SwapSourceIndex = null;
            }
            else
            {
                ShowSummary = true;
                summaryAge = 0f;
            }
            AudioManager.PlaySound("select");
        }
        else if (InputManager.IsActionPressed(GameAction.Run) && !SwapSourceIndex.HasValue)
        {
            SwapSourceIndex = SelectedIndex;
            AudioManager.PlaySound("select");
        }
    }

    public void Draw(int screenWidth, int screenHeight, Party party)
    {
        if (!IsActive) return;

        if (ShowSummary && SelectedIndex < party.Count)
        {
            ModernUi.DrawSummary(screenWidth, screenHeight, party.Members[SelectedIndex], SelectedIndex, party.Count,
                Math.Clamp(summaryAge / SummaryAppearTime, 0f, 1f));
        }
        else
        {
            ModernUi.DrawParty(screenWidth, screenHeight, party, SelectedIndex, SwapSourceIndex, Math.Clamp(openAge / AppearTime, 0f, 1f));
        }
    }
}
