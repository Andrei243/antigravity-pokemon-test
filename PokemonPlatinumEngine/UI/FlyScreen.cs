using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Where Fly can go (plan 02 · S2; style guide, "Fly"): the towns the player has arrived in, in the original's
/// order, on the map of Sinnoh. Its logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>,
/// <see cref="Cancel"/>), so tests and the harness drive it; the town chosen is handed to the game with
/// <see cref="TakeChoice"/>.
/// </summary>
public sealed class FlyScreen
{
    private const float AppearTime = 0.35f;

    private SpawnLocation? chosen;
    private float age;

    public bool IsActive { get; private set; }
    public IReadOnlyList<SpawnLocation> Towns { get; private set; } = Array.Empty<SpawnLocation>();
    public int Cursor { get; private set; }

    /// <summary>The first town the list shows, and how many it shows.</summary>
    public int First { get; private set; }
    public const int VisibleRows = 9;

    /// <summary>The tile of the map of Sinnoh the player stands on (or went in from), ringed on the map; null indoors with no way to tell.</summary>
    public (int X, int Y)? Here { get; private set; }

    /// <summary>Which of the team uses Fly: its place in the party.</summary>
    public int Flier { get; private set; }

    /// <summary>Opens on the towns to choose from, with the cursor on the one nearest the player.</summary>
    public void Open(IEnumerable<SpawnLocation> towns, (int X, int Y)? here, int flier)
    {
        Towns = towns.ToList();
        Here = here;
        Flier = flier;
        Cursor = 0;
        if (here is var (x, y) && Towns.Count > 0)
            Cursor = Towns.Select((t, i) => (Distance: Math.Abs(t.X - x) + Math.Abs(t.Y - y), i)).Min().i;
        First = UiNav.Window(0, Cursor, Towns.Count, VisibleRows);
        chosen = null;
        age = 0f;
        IsActive = true;
    }

    public void Move(int step)
    {
        if (Towns.Count == 0 || step == 0) return;
        Cursor = UiNav.Wrap(Cursor, step, Towns.Count);
        First = UiNav.Window(First, Cursor, Towns.Count, VisibleRows);
        AudioManager.PlaySound("cursor");
    }

    public void Confirm()
    {
        if (Towns.Count == 0) return;
        chosen = Towns[Cursor];
        IsActive = false;
        AudioManager.PlaySound("select");
    }

    public void Cancel()
    {
        IsActive = false;
        AudioManager.PlaySound("cancel");
    }

    /// <summary>The town chosen, once.</summary>
    public SpawnLocation? TakeChoice()
    {
        var town = chosen;
        chosen = null;
        return town;
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        age += dt;
        if (InputManager.IsActionPressed(GameAction.Down) || InputManager.IsActionPressed(GameAction.Right)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Up) || InputManager.IsActionPressed(GameAction.Left)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    public void Draw(int sw, int sh) =>
        ModernUi.DrawFly(sw, sh, Towns, Cursor, First, Here, Math.Clamp(age / AppearTime, 0f, 1f));
}
