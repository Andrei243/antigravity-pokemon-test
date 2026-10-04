using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Pokédex: every species in number order as a list, and the chosen one beside it with as much as the
/// player has learned of it (nothing, what was seen, or everything once it has been caught). Its logic takes
/// no input (<see cref="Move"/>), so tests and the harness drive it. Sinnoh's own numbering, sorting, search
/// and the area view are plan 03 · D10's.
/// </summary>
public class PokedexScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>How many species show at once, and how far left and right jump.</summary>
    public const int VisibleRows = 8, Jump = 10;

    private readonly List<PokemonSpecies> allSpecies = new();
    private float openAge;

    public int SelectedIndex { get; set; }
    public int FirstRow { get; private set; }
    public bool IsActive { get; set; }

    public IReadOnlyList<PokemonSpecies> Species => allSpecies;

    /// <summary>Opens on the first species the player has seen, so the list doesn't begin with a page of dashes.</summary>
    public void Open(Pokedex? pokedex = null)
    {
        IsActive = true;
        openAge = 0f;
        allSpecies.Clear();
        allSpecies.AddRange(PokemonDatabase.GetAll().OrderBy(s => s.DexNumber));
        int firstSeen = pokedex == null ? -1 : allSpecies.FindIndex(s => pokedex.IsSeen(s.DexNumber));
        SelectedIndex = Math.Max(0, firstSeen);
        FirstRow = 0;
        Follow();
    }

    public void Close() => IsActive = false;

    /// <summary>
    /// One step wraps from the last species to the first; a jump of ten stops at either end.
    /// </summary>
    public void Move(int step)
    {
        if (step == 0 || allSpecies.Count == 0) return;
        int next = Math.Abs(step) == 1
            ? UiNav.Wrap(SelectedIndex, step, allSpecies.Count)
            : Math.Clamp(SelectedIndex + step, 0, allSpecies.Count - 1);
        if (next == SelectedIndex) return;
        SelectedIndex = next;
        Follow();
        AudioManager.PlaySound("cursor");
    }

    private void Follow()
    {
        SelectedIndex = Math.Clamp(SelectedIndex, 0, Math.Max(0, allSpecies.Count - 1));
        FirstRow = UiNav.Window(FirstRow, SelectedIndex, allSpecies.Count, VisibleRows);
    }

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;

        if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Left)) Move(-Jump);
        else if (InputManager.IsActionPressed(GameAction.Right)) Move(Jump);
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu))
        {
            Close();
            AudioManager.PlaySound("cancel");
        }
    }

    public void Draw(int screenWidth, int screenHeight, Pokedex pokedex)
    {
        if (!IsActive) return;
        Follow();
        ModernUi.DrawPokedex(screenWidth, screenHeight, this, pokedex, Math.Clamp(openAge / AppearTime, 0f, 1f));
    }
}
