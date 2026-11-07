using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The kit's tabs (plan 39 · CR1): the pouch of materials. CR3 adds MAKE, CR4 the TMs, CR5 the crosses.</summary>
public enum CraftingTab { Materials }

/// <summary>
/// The crafting kit looked into (plan 39 · CR1; style guide, "Menu screens (G10)", "The kit"): a strip of tabs, and under
/// it the list of every material with how many the pouch holds (the ones held first in the eye, the others dimmed) and,
/// for the one under the cursor, what it is, where it comes from and what it is worth. Up and down move the cursor,
/// left and right change the tab (there is one so far), B closes. Its steps take no input of their own, so tests and
/// the harness drive it; the list is built with no window.
/// </summary>
public sealed class CraftingScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>The rows the list shows: as many as its panel holds (<see cref="ModernUi.RowsIn"/>).</summary>
    public static int VisibleRows => ModernUi.RowsIn(ModernUi.ContentBottom - (ModernUi.ContentTop + 76 + 24));

    private CraftingState state = new();
    private float age;

    public bool IsActive { get; private set; }

    /// <summary>The tabs the kit has now, in order.</summary>
    public static IReadOnlyList<CraftingTab> Tabs { get; } = new[] { CraftingTab.Materials };

    public CraftingTab Tab { get; private set; }

    /// <summary>The material under the cursor, and the first row showing.</summary>
    public int Cursor { get; private set; }
    public int First { get; private set; }

    public CraftingState State => state;

    public MaterialPouch Pouch => state.Pouch;

    /// <summary>Every material, in the table's order: a Pokémon's by type, then the field's, then the pantry's.</summary>
    public IReadOnlyList<MaterialData> Entries => MaterialTable.All;

    public MaterialData? Selected => Cursor >= 0 && Cursor < Entries.Count ? Entries[Cursor] : null;

    /// <summary>How far the screen has slid into place, 0 to 1.</summary>
    public float Appear => Math.Clamp(age / AppearTime, 0f, 1f);

    public void Open(CraftingState from)
    {
        state = from;
        IsActive = true;
        Tab = CraftingTab.Materials;
        // The cursor goes to the first thing held, where there is one
        int held = Entries.ToList().FindIndex(m => from.Pouch.Count(m.Id) > 0);
        Cursor = Math.Max(0, held);
        First = UiNav.Window(0, Cursor, Entries.Count, VisibleRows);
        age = 0f;
        AudioManager.PlaySound("menu_open");
    }

    public void Close()
    {
        IsActive = false;
        AudioManager.PlaySound("menu_close");
    }

    /// <summary>The cursor up (−1) or down (+1), wrapping round.</summary>
    public void Move(int delta)
    {
        if (delta == 0 || Entries.Count == 0) return;
        int next = UiNav.Wrap(Cursor, Math.Sign(delta), Entries.Count);
        if (next == Cursor) return;
        Cursor = next;
        First = UiNav.Window(First, Cursor, Entries.Count, VisibleRows);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The next tab (+1) or the one before (−1), wrapping round; nothing while there is one.</summary>
    public void ChangeTab(int delta)
    {
        if (Tabs.Count < 2) return;
        Tab = Tabs[UiNav.Wrap(Array.IndexOf(Tabs.ToArray(), Tab), Math.Sign(delta), Tabs.Count)];
        AudioManager.PlaySound("cursor");
    }

    public void Cancel() => Close();

    public void Update(float dt)
    {
        if (!IsActive) return;
        age += dt;
        if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Left)) ChangeTab(-1);
        else if (InputManager.IsActionPressed(GameAction.Right)) ChangeTab(1);
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawCrafting(screenWidth, screenHeight, this, Appear);
    }
}
