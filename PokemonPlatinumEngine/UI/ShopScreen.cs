using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// A Poké Mart's counter: the stock as a list with prices, the player's money, the chosen item beside it, and
/// "how many?" before anything is bought. Its logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>,
/// <see cref="Cancel"/>), so tests and the harness drive it. Up or down kept down runs on through the stock
/// (<see cref="HeldKey"/>). What a shop stocks and selling to it are plan 06 · R11's.
/// </summary>
public class ShopScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>How many rows of the stock show at once, and the most of one item bought at a time.</summary>
    public const int VisibleRows = 9, MostAtOnce = 99;

    private readonly List<ItemData> stock = new();
    private readonly UiReveal asking = new(0.18f, 0.1f);
    private readonly HeldKey upDown = new();
    private readonly CursorTick tick = new();
    private float openAge;

    public int SelectedIndex { get; set; }
    public int FirstRow { get; private set; }
    public bool IsActive { get; set; }

    /// <summary>The name over the door: what the screen is headed with.</summary>
    public string Name { get; private set; } = "POKÉ MART";

    public IReadOnlyList<ItemData> Stock => stock;

    /// <summary>True while "how many?" is up, and the number chosen so far.</summary>
    public bool ChoosingQuantity { get; private set; }
    public int Quantity { get; private set; } = 1;

    public ItemData? Selected => SelectedIndex >= 0 && SelectedIndex < stock.Count ? stock[SelectedIndex] : null;

    public void Open(string? name = null)
    {
        IsActive = true;
        SelectedIndex = 0;
        FirstRow = 0;
        ChoosingQuantity = false;
        asking.Snap(false);
        openAge = 0f;
        upDown.Release();
        Name = string.IsNullOrWhiteSpace(name) ? "POKÉ MART" : name.ToUpperInvariant();
        stock.Clear();
        foreach (string item in new[] { "Poké Ball", "Great Ball", "Potion", "Super Potion", "Antidote", "Revive" })
            if (ItemDatabase.Get(item) is { } data) stock.Add(data);
        AudioManager.PlaySound("select");
    }

    public void Close()
    {
        IsActive = false;
        ChoosingQuantity = false;
    }

    /// <summary>The most of an item this money buys at once: none if even one is too dear.</summary>
    public static int MostAffordable(ItemData item, int money) =>
        item.Price <= 0 ? MostAtOnce : Math.Clamp(money / item.Price, 0, MostAtOnce);

    /// <summary>
    /// Up and down the stock; while "how many?" is up, left and right change the number by one and up and down
    /// by ten, wrapping between one and the most the money buys. In the stock a step that comes from a key kept
    /// down (<paramref name="held"/>) stops at either end instead of wrapping.
    /// </summary>
    public void Move(int dx, int dy, int money, bool held = false)
    {
        if (dx == 0 && dy == 0) return;
        if (ChoosingQuantity)
        {
            int most = Math.Max(1, MostAffordable(Selected!, money));
            int next = Quantity + dx - dy * 10;
            // Past either end it wraps round; a jump of ten stops at the end first
            if (next > most) next = Quantity == most ? 1 : most;
            else if (next < 1) next = Quantity == 1 ? most : 1;
            if (next != Quantity) AudioManager.PlaySound("cursor");
            Quantity = next;
            return;
        }
        if (dy == 0 || stock.Count == 0) return;
        int row = held ? Math.Clamp(SelectedIndex + dy, 0, stock.Count - 1) : UiNav.Wrap(SelectedIndex, dy, stock.Count);
        if (held && row == SelectedIndex) return;
        SelectedIndex = row;
        FirstRow = UiNav.Window(FirstRow, SelectedIndex, stock.Count, VisibleRows);
        if (tick.Sounds(held)) AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// The A button: on an item, asks how many (or says the money doesn't reach); on the number, buys them.
    /// Returns what was spent.
    /// </summary>
    public int Confirm(Inventory inventory, int money, Action<string> onNotification)
    {
        if (Selected is not { } item) return 0;
        if (!ChoosingQuantity)
        {
            if (MostAffordable(item, money) < 1)
            {
                onNotification("You don't have enough money.");
                AudioManager.PlaySound("cancel");
                return 0;
            }
            ChoosingQuantity = true;
            Quantity = 1;
            asking.Open();
            AudioManager.PlaySound("select");
            return 0;
        }

        int count = Math.Clamp(Quantity, 1, Math.Max(1, MostAffordable(item, money)));
        int cost = count * item.Price;
        ChoosingQuantity = false;
        asking.Close();
        if (cost > money) return 0;
        inventory.AddItem(item, count);
        AudioManager.PlaySound("select");
        onNotification(count == 1 ? $"Bought a {item.Name}." : $"Bought {count} × {item.Name}.");
        return cost;
    }

    /// <summary>The B button: out of "how many?", then out of the shop.</summary>
    public void Cancel()
    {
        if (ChoosingQuantity)
        {
            ChoosingQuantity = false;
            asking.Close();
        }
        else Close();
        AudioManager.PlaySound("cancel");
    }

    public void Update(Inventory playerInventory, ref int playerMoney, Action<string> onNotification, float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        asking.Update(dt);
        tick.Update(dt);

        // Up or down kept down runs on through the stock; "how many?" takes presses only
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right);
        int dy = upDown.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down),
            ChoosingQuantity ? 0 : InputManager.Axis(GameAction.Up, GameAction.Down, held: true));
        // More than one step in a frame only ever comes from a key held in the stock
        for (int i = 0; i < Math.Max(Math.Abs(dx), Math.Abs(dy)); i++) Move(dx, Math.Sign(dy), playerMoney, upDown.Repeating);

        // A button pressed while the list runs on still counts: it acts on the item the cursor has come to
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) playerMoney -= Confirm(playerInventory, playerMoney, onNotification);
    }

    public void Draw(int screenWidth, int screenHeight, int playerMoney, Inventory inventory)
    {
        if (!IsActive) return;
        FirstRow = UiNav.Window(FirstRow, SelectedIndex, stock.Count, VisibleRows);
        ModernUi.DrawShop(screenWidth, screenHeight, this, playerMoney, inventory, Math.Clamp(openAge / AppearTime, 0f, 1f), asking.Shown, asking.Visible);
    }
}
