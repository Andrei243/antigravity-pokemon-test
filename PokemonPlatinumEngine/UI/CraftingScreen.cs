using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The kit's tabs (plan 39): the pouch of materials (CR1), the TM maker (CR4, only at a PC), the recipes (CR3) and the
/// pantry's goods for sale (CR6, only at the workshop's counter). CR5 adds the crosses.
/// </summary>
public enum CraftingTab { Materials, Machines, Make, Pantry }

/// <summary>Where the kit is opened: anywhere (the bag, the item button), at a PC's TM maker, or at the workshop's pantry counter.</summary>
public enum CraftingPlace { Anywhere, Maker, Pantry }

/// <summary>
/// The crafting kit looked into (plan 39 · CR1; style guide, "Menu screens (G10)", "The kit"): a strip of tabs, and under
/// it the list of every material with how many the pouch holds (the ones held first in the eye, the others dimmed) and,
/// for the one under the cursor, what it is, where it comes from and what it is worth. Up and down move the cursor,
/// left and right change the tab, B closes. At a PC (<c>craft tm</c>) a second tab lists every TM and TR the player has
/// had in the bag with what making it again takes against what is held (plan 39 · CR4); A makes the one under the
/// cursor. MAKE (plan 39 · CR3) lists the kit's recipes with what each takes against what is held: A asks how many
/// (up and down by one, left and right by ten) and A again makes them; at the workshop's counter (<c>craft pantry</c>,
/// plan 39 · CR6) PANTRY sells the cooking goods into the pouch the same way, the money taken back by
/// <see cref="TakeSpent"/>. Its steps take no input of their own, so tests and the harness drive it; the lists are built
/// with no window.
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

    /// <summary>Where the kit was opened.</summary>
    public CraftingPlace Place { get; private set; }

    /// <summary>The tabs the kit has now, in order: the materials and MAKE everywhere, the TM maker's only at a PC, the pantry's only at its counter.</summary>
    public IReadOnlyList<CraftingTab> Tabs => Place switch
    {
        CraftingPlace.Maker => new[] { CraftingTab.Materials, CraftingTab.Make, CraftingTab.Machines },
        CraftingPlace.Pantry => new[] { CraftingTab.Materials, CraftingTab.Make, CraftingTab.Pantry },
        _ => new[] { CraftingTab.Materials, CraftingTab.Make }
    };

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

    /// <summary>The kit's recipes (plan 39 · CR3), in the table's order, and the cursor and first row of their list.</summary>
    public IReadOnlyList<CraftRecipe> Recipes => Crafting.Recipes;
    public int RecipeCursor { get; private set; }
    public int RecipeFirst { get; private set; }

    public CraftRecipe? SelectedRecipe => RecipeCursor >= 0 && RecipeCursor < Recipes.Count ? Recipes[RecipeCursor] : null;

    /// <summary>The pantry's goods (plan 39 · CR6), sold at the workshop's counter for their worth, and the cursor and first row of their list.</summary>
    public IReadOnlyList<MaterialData> Pantry => pantry ??= MaterialTable.All.Where(m => m.Kind == MaterialKind.Pantry).ToList();
    private List<MaterialData>? pantry;
    public int PantryCursor { get; private set; }
    public int PantryFirst { get; private set; }

    public MaterialData? SelectedGood => PantryCursor >= 0 && PantryCursor < Pantry.Count ? Pantry[PantryCursor] : null;

    /// <summary>"How many?" on MAKE or the pantry: the number asked (one or more), or nought while nothing is being asked.</summary>
    public int HowMany { get; private set; }

    /// <summary>The most "how many?" may come to now: the times the recipe can be made, or the goods the purse and the pouch take.</summary>
    public int MostNow => Tab switch
    {
        CraftingTab.Make when SelectedRecipe is { } recipe => Crafting.MostOf(recipe, bag, state.Pouch),
        CraftingTab.Pantry when SelectedGood is { } good => Math.Max(0, Math.Min(Crafting.MostAtOnce,
            Math.Min(money / Math.Max(1, good.Worth), MaterialTable.Limit - state.Pouch.Count(good.Id)))),
        _ => 0
    };

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
    public void Open(CraftingState from) => Open(from, new Inventory(), 0, CraftingPlace.Anywhere);

    /// <summary>Opens the kit with the bag it makes into and the money held; at a PC it opens on the TM maker's tab.</summary>
    public void Open(CraftingState from, Inventory into, int held, bool atMaker) =>
        Open(from, into, held, atMaker ? CraftingPlace.Maker : CraftingPlace.Anywhere);

    /// <summary>
    /// Opens the kit; at a PC (<paramref name="atMaker"/>) it opens on the TM maker, with the bag it makes into and the
    /// money the player holds, and the money spent is taken back by <see cref="TakeSpent"/>.
    /// </summary>
    public void Open(CraftingState from, Inventory into, int held, CraftingPlace place)
    {
        state = from;
        bag = into;
        money = held;
        spent = 0;
        Place = place;
        AtMaker = place == CraftingPlace.Maker;
        Notice = "";
        LastMade = false;
        IsActive = true;
        Tab = place switch { CraftingPlace.Maker => CraftingTab.Machines, CraftingPlace.Pantry => CraftingTab.Pantry, _ => CraftingTab.Materials };
        Machines = AtMaker ? TmMaker.Known(into) : Array.Empty<ItemData>();
        MachineCursor = 0;
        MachineFirst = 0;
        RecipeCursor = 0;
        RecipeFirst = 0;
        PantryCursor = 0;
        PantryFirst = 0;
        HowMany = 0;
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

    /// <summary>
    /// A at the kit: on MAKE or the pantry, "how many?" is asked (or, while it is, the recipe is made or the goods bought
    /// that many times); at the maker the machine under the cursor is made (materials and money taken, one machine into the
    /// bag); or the reason it can't be.
    /// </summary>
    public void Confirm()
    {
        if (Tab is CraftingTab.Make or CraftingTab.Pantry)
        {
            ConfirmCounted();
            return;
        }
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

    // MAKE and the pantry: the first A asks how many, the second makes or buys that many
    private void ConfirmCounted()
    {
        if (HowMany == 0)
        {
            if (Tab == CraftingTab.Make && SelectedRecipe is { } recipe)
            {
                if (Crafting.WhyNot(recipe, 1, bag, state.Pouch) is { } why) { Refuse(why); return; }
            }
            else if (Tab == CraftingTab.Pantry && SelectedGood is { } good)
            {
                if (money < good.Worth) { Refuse(Text.Of("crafting.pantry.why.money")); return; }
                if (state.Pouch.IsFull(good.Id)) { Refuse(Text.Of("crafting.pantry.why.full", ("name", MaterialTable.NameOf(good.Id)))); return; }
            }
            else return;
            HowMany = 1;
            Notice = "";
            AudioManager.PlaySound("select");
            return;
        }
        int times = Math.Min(HowMany, MostNow);
        HowMany = 0;
        if (Tab == CraftingTab.Make && SelectedRecipe is { } making)
        {
            var made = Crafting.Make(making, times, bag, state.Pouch);
            LastMade = made.Made;
            if (!made.Made) { Refuse(made.Why ?? ""); return; }
            Notice = Text.Of("crafting.make.made", ("name", Crafting.ItemOf(making)!.DisplayName), ("n", made.Count));
            AudioManager.PlaySound("select");
        }
        else if (Tab == CraftingTab.Pantry && SelectedGood is { } buying && times > 0)
        {
            int added = state.Pouch.Add(buying.Id, times);
            money -= buying.Worth * added;
            spent += buying.Worth * added;
            LastMade = added > 0;
            Notice = Text.Of("crafting.pantry.bought", ("name", MaterialTable.NameOf(buying.Id)), ("n", added), ("money", buying.Worth * added));
            AudioManager.PlaySound("select");
        }
    }

    private void Refuse(string why)
    {
        LastMade = false;
        Notice = why;
        AudioManager.PlaySound("error");
    }

    /// <summary>While "how many?" is asked: the number up or down by <paramref name="delta"/>, kept between one and what can be made or bought now.</summary>
    public void ChangeAmount(int delta)
    {
        if (HowMany == 0 || delta == 0) return;
        int next = Math.Clamp(HowMany + delta, 1, Math.Max(1, MostNow));
        if (next == HowMany) return;
        HowMany = next;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The cursor up (−1) or down (+1), wrapping round.</summary>
    public void Move(int delta)
    {
        if (Tab is CraftingTab.Make or CraftingTab.Pantry)
        {
            bool make = Tab == CraftingTab.Make;
            int count = make ? Recipes.Count : Pantry.Count;
            if (delta == 0 || count == 0 || HowMany > 0) return;
            int at = make ? RecipeCursor : PantryCursor;
            int moved = UiNav.Wrap(at, Math.Sign(delta), count);
            if (moved == at) return;
            if (make) { RecipeCursor = moved; RecipeFirst = UiNav.Window(RecipeFirst, moved, count, VisibleRows); }
            else { PantryCursor = moved; PantryFirst = UiNav.Window(PantryFirst, moved, count, VisibleRows); }
            Notice = "";
            AudioManager.PlaySound("cursor");
            return;
        }
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
        if (tabs.Count < 2 || HowMany > 0) return;
        Tab = tabs[UiNav.Wrap(tabs.ToList().IndexOf(Tab), Math.Sign(delta), tabs.Count)];
        Notice = "";
        AudioManager.PlaySound("cursor");
    }

    /// <summary>B: puts "how many?" away, or closes the kit.</summary>
    public void Cancel()
    {
        if (HowMany > 0)
        {
            HowMany = 0;
            AudioManager.PlaySound("cancel");
            return;
        }
        Close();
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        age += dt;
        if (HowMany > 0)
        {
            if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
            else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
            else if (InputManager.IsActionPressed(GameAction.Up)) ChangeAmount(1);
            else if (InputManager.IsActionPressed(GameAction.Down)) ChangeAmount(-1);
            else if (InputManager.IsActionPressed(GameAction.Right)) ChangeAmount(10);
            else if (InputManager.IsActionPressed(GameAction.Left)) ChangeAmount(-10);
            return;
        }
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
