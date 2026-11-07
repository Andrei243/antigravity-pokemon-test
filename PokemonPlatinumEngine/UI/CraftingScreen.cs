using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The kit's tabs (plan 39): the pouch of materials (CR1) and the TM maker (CR4, only at a PC). CR3 adds MAKE, CR5 the crosses.</summary>
public enum CraftingTab { Materials, Machines }

/// <summary>
/// The crafting kit looked into (plan 39 · CR1; style guide, "Menu screens (G10)", "The kit"): a strip of tabs, and under
/// it the list of every material with how many the pouch holds (the ones held first in the eye, the others dimmed) and,
/// for the one under the cursor, what it is, where it comes from and what it is worth. Up and down move the cursor,
/// left and right change the tab, B closes. At a PC (<c>craft tm</c>) a second tab lists every TM and TR the player has
/// had in the bag with what making it again takes against what is held (plan 39 · CR4); A makes the one under the
/// cursor. Its steps take no input of their own, so tests and the harness drive it; the lists are built with no window.
/// </summary>
public sealed class CraftingScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>The rows the list shows: as many as its panel holds (<see cref="ModernUi.RowsIn"/>).</summary>
    public static int VisibleRows => ModernUi.RowsIn(ModernUi.ContentBottom - (ModernUi.ContentTop + 76 + 24));

    private CraftingState state = new();
    private Inventory bag = new();
    private int money;
    private int spent;
    private float age;

    public bool IsActive { get; private set; }

    /// <summary>Whether the kit was opened at a PC, where it makes TMs.</summary>
    public bool AtMaker { get; private set; }

    /// <summary>The tabs the kit has now, in order: the TM maker's only at a PC.</summary>
    public IReadOnlyList<CraftingTab> Tabs => AtMaker ? new[] { CraftingTab.Materials, CraftingTab.Machines } : new[] { CraftingTab.Materials };

    public CraftingTab Tab { get; private set; }

    /// <summary>The material under the cursor, and the first row showing.</summary>
    public int Cursor { get; private set; }
    public int First { get; private set; }

    public CraftingState State => state;

    public MaterialPouch Pouch => state.Pouch;

    /// <summary>Every material, in the table's order: a Pokémon's by type, then the field's, then the pantry's.</summary>
    public IReadOnlyList<MaterialData> Entries => MaterialTable.All;

    public MaterialData? Selected => Cursor >= 0 && Cursor < Entries.Count ? Entries[Cursor] : null;

    /// <summary>The machines the player has had (plan 39 · CR4), TMs and then TRs by number, and the cursor and first row of their list.</summary>
    public IReadOnlyList<ItemData> Machines { get; private set; } = Array.Empty<ItemData>();
    public int MachineCursor { get; private set; }
    public int MachineFirst { get; private set; }

    public ItemData? SelectedMachine => MachineCursor >= 0 && MachineCursor < Machines.Count ? Machines[MachineCursor] : null;

    /// <summary>What making the machine under the cursor takes; null for none.</summary>
    public TmCost? SelectedCost => SelectedMachine is { } machine ? TmMaker.CostOf(machine) : null;

    /// <summary>The bag the machines are made into, and the money the player holds (less what making them has cost).</summary>
    public Inventory Bag => bag;
    public int Money => money;

    /// <summary>What the last press at the maker said (a machine made, or why not); empty when nothing has been pressed.</summary>
    public string Notice { get; private set; } = "";

    /// <summary>Whether the last press at the maker made a machine.</summary>
    public bool LastMade { get; private set; }

    /// <summary>How far the screen has slid into place, 0 to 1.</summary>
    public float Appear => Math.Clamp(age / AppearTime, 0f, 1f);

    /// <summary>Opens the kit to look into anywhere: the pouch alone.</summary>
    public void Open(CraftingState from) => Open(from, new Inventory(), 0, atMaker: false);

    /// <summary>
    /// Opens the kit; at a PC (<paramref name="atMaker"/>) it opens on the TM maker, with the bag it makes into and the
    /// money the player holds, and the money spent is taken back by <see cref="TakeSpent"/>.
    /// </summary>
    public void Open(CraftingState from, Inventory into, int held, bool atMaker)
    {
        state = from;
        bag = into;
        money = held;
        spent = 0;
        AtMaker = atMaker;
        Notice = "";
        LastMade = false;
        IsActive = true;
        Tab = atMaker ? CraftingTab.Machines : CraftingTab.Materials;
        Machines = atMaker ? TmMaker.Known(into) : Array.Empty<ItemData>();
        MachineCursor = 0;
        MachineFirst = 0;
        // The cursor goes to the first thing held, where there is one
        int held1 = Entries.ToList().FindIndex(m => from.Pouch.Count(m.Id) > 0);
        Cursor = Math.Max(0, held1);
        First = UiNav.Window(0, Cursor, Entries.Count, VisibleRows);
        age = 0f;
        AudioManager.PlaySound("menu_open");
    }

    public void Close()
    {
        IsActive = false;
        AudioManager.PlaySound("menu_close");
    }

    /// <summary>The money the machines made since this was last asked cost, once: the engine takes it from the player's purse.</summary>
    public int TakeSpent()
    {
        int taken = spent;
        spent = 0;
        return taken;
    }

    /// <summary>Makes the machine under the cursor (A at the maker): materials and money taken, one machine into the bag; or the reason it can't be.</summary>
    public void Confirm()
    {
        if (Tab != CraftingTab.Machines || SelectedMachine is not { } machine) return;
        var made = TmMaker.Make(machine, bag, state.Pouch, money);
        LastMade = made.Made;
        if (!made.Made)
        {
            Notice = made.Why ?? "";
            AudioManager.PlaySound("error");
            return;
        }
        money -= made.MoneySpent;
        spent += made.MoneySpent;
        Notice = Text.Of("crafting.tm.made", ("name", machine.DisplayName), ("move", LocalNames.Move(machine.TeachesMove!)));
        AudioManager.PlaySound("select");
    }

    /// <summary>The cursor up (−1) or down (+1), wrapping round.</summary>
    public void Move(int delta)
    {
        if (Tab == CraftingTab.Machines)
        {
            if (delta == 0 || Machines.Count == 0) return;
            int nextMachine = UiNav.Wrap(MachineCursor, Math.Sign(delta), Machines.Count);
            if (nextMachine == MachineCursor) return;
            MachineCursor = nextMachine;
            MachineFirst = UiNav.Window(MachineFirst, MachineCursor, Machines.Count, VisibleRows);
            Notice = "";
            AudioManager.PlaySound("cursor");
            return;
        }
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
        var tabs = Tabs;
        if (tabs.Count < 2) return;
        Tab = tabs[UiNav.Wrap(tabs.ToList().IndexOf(Tab), Math.Sign(delta), tabs.Count)];
        Notice = "";
        AudioManager.PlaySound("cursor");
    }

    public void Cancel() => Close();

    public void Update(float dt)
    {
        if (!IsActive) return;
        age += dt;
        if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
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
