using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The PC's HALL OF FAME (plan 06 · R12; the original's <c>pc_hall_of_fame</c>): the teams that entered it, newest
/// first, each under its number and day. Left and right go to an older or a newer team (round from the last to the
/// first), up and down through the team's Pokémon and on into the next team's, and A turns the panel between who a
/// Pokémon is and the moves it knew. Its logic takes no input (<see cref="Move"/>, <see cref="Turn"/>), so tests and
/// the harness drive it.
/// </summary>
public class HallOfFameScreen
{
    private const float AppearTime = 0.3f;
    private float openAge;

    public bool IsActive { get; private set; }

    /// <summary>Which team, counted from the newest (0), and which of its Pokémon.</summary>
    public int Entry { get; private set; }
    public int Member { get; private set; }

    /// <summary>Whether the panel shows the moves instead of who the Pokémon is.</summary>
    public bool ShowingMoves { get; private set; }

    public void Open()
    {
        IsActive = true;
        Entry = 0;
        Member = 0;
        ShowingMoves = false;
        openAge = 0f;
    }

    public void Close() => IsActive = false;

    /// <summary>The team shown and its number, or null when nobody has entered yet.</summary>
    public (int Number, HallOfFameEntry Entry)? Shown(HallOfFame records) =>
        records.NewestFirst().Skip(Entry).Select(e => ((int, HallOfFameEntry)?)e).FirstOrDefault();

    /// <summary>Left and right: an older (+1) or newer (−1) team, round from the oldest to the newest; up and down: the next Pokémon, on into the next team.</summary>
    public void Move(int dx, int dy, HallOfFame records)
    {
        int teams = records.Entries.Count;
        if (teams == 0) return;
        if (dx != 0)
        {
            Entry = ((Entry + Math.Sign(dx)) % teams + teams) % teams;
            Member = 0;
        }
        else if (dy != 0)
        {
            int size = Shown(records)!.Value.Entry.Team.Count;
            Member += Math.Sign(dy);
            if (Member >= size)
            {
                Entry = (Entry + 1) % teams;
                Member = 0;
            }
            else if (Member < 0)
            {
                Entry = ((Entry - 1) % teams + teams) % teams;
                Member = Shown(records)!.Value.Entry.Team.Count - 1;
            }
        }
        else return;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>A: the panel turns between the Pokémon and its moves.</summary>
    public void Turn()
    {
        ShowingMoves = !ShowingMoves;
        AudioManager.PlaySound("page");
    }

    public void Update(HallOfFame records, float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        int dx = (InputManager.IsActionPressed(GameAction.Right) ? -1 : 0) + (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
        int dy = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
        if (dx != 0 || dy != 0) Move(dx, dy, records);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Turn();
        else if (InputManager.IsActionPressed(GameAction.Cancel))
        {
            Close();
            AudioManager.PlaySound("cancel");
        }
    }

    public void Draw(int screenWidth, int screenHeight, HallOfFame records)
    {
        if (!IsActive) return;
        ModernUi.DrawHallOfFame(screenWidth, screenHeight, this, records, Math.Clamp(openAge / AppearTime, 0f, 1f));
    }
}
