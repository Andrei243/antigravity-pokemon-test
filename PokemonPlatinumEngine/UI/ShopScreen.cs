using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the counter is: the first question, the stock to buy from, or the bag's items to sell.</summary>
public enum ShopMode { Choosing, Buying, Selling }

/// <summary>
/// A Poké Mart's counter (plan 06 · R11): BUY, SELL or SEE YA!, then the counter's own stock (<see cref="MartDatabase"/>:
/// the common list by the badges, or the town's specialties) or the bag's items, each with its price, the
/// player's money and the chosen item beside them, and "how many?" before anything changes hands. An item sells
/// for half its price; a key item, or one with no price, can't be sold. Its logic takes no input
/// (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and the harness drive it. Up or down
/// kept down runs on through the list (<see cref="HeldKey"/>).
/// </summary>
public class ShopScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>How many rows of the list show at once, and the most of one item bought at a time.</summary>
    public const int VisibleRows = 9, MostAtOnce = 99;

    /// <summary>The first question's answers, in their order.</summary>
    public static readonly string[] Choices = { "BUY", "SELL", "SEE YA!" };

    private readonly List<ItemData> stock = new();
    private readonly List<ItemData> selling = new();
    private readonly UiReveal asking = new(0.18f, 0.1f);
    private readonly HeldKey upDown = new();
    private readonly CursorTick tick = new();
    private float openAge;

    public ShopMode Mode { get; private set; }

    /// <summary>The cursor on BUY, SELL and SEE YA! while the first question is up.</summary>
    public int ChoiceIndex { get; private set; }

    public int SelectedIndex { get; set; }
    public int FirstRow { get; private set; }
    public bool IsActive { get; set; }

    /// <summary>The name over the door: what the screen is headed with.</summary>
    public string Name { get; private set; } = "POKÉ MART";

    public IReadOnlyList<ItemData> Stock => stock;

    /// <summary>What the list shows now: the stock while buying, the bag's items that sell while selling.</summary>
    public IReadOnlyList<ItemData> Listed => Mode == ShopMode.Selling ? selling : stock;

    /// <summary>True while "how many?" is up, and the number chosen so far.</summary>
    public bool ChoosingQuantity { get; private set; }
    public int Quantity { get; private set; } = 1;

    public ItemData? Selected => SelectedIndex >= 0 && SelectedIndex < Listed.Count ? Listed[SelectedIndex] : null;

    /// <summary>What a shop pays for one of an item: half its price, nothing for a key item.</summary>
    public static int SellPrice(ItemData item) => item.Pocket == ItemPocket.KeyItems ? 0 : item.Price / 2;

    public static bool CanSell(ItemData item) => SellPrice(item) > 0;

    /// <summary>
    /// Opens the counter on its first question, with the stock it sells (none given: the common list with no
    /// badges). <paramref name="start"/> opens it straight on a list instead.
    /// </summary>
    public void Open(string? name = null, IEnumerable<ItemData>? sells = null, ShopMode start = ShopMode.Choosing)
    {
        IsActive = true;
        ChoiceIndex = 0;
        SelectedIndex = 0;
        FirstRow = 0;
        ChoosingQuantity = false;
        asking.Snap(false);
        openAge = 0f;
        upDown.Release();
        Name = string.IsNullOrWhiteSpace(name) ? "POKÉ MART" : name.ToUpperInvariant();
        stock.Clear();
        stock.AddRange(sells ?? MartDatabase.Stock(null, 0));
        selling.Clear();
        Mode = start;
        AudioManager.PlaySound("select");
    }

    public void Close()
    {
        IsActive = false;
        ChoosingQuantity = false;
    }

    /// <summary>The bag's items that a shop buys, pocket by pocket in the bag's order.</summary>
    public static List<ItemData> Sellable(Inventory inventory) =>
        BagScreen.Pockets.SelectMany(p => inventory.GetPocketItems(p)).Select(s => s.Data).Where(CanSell).ToList();

    /// <summary>The most of an item this money buys at once: none if even one is too dear.</summary>
    public static int MostAffordable(ItemData item, int money) =>
        item.Price <= 0 ? MostAtOnce : Math.Clamp(money / item.Price, 0, MostAtOnce);

    private int MostOf(ItemData item, int money, Inventory? inventory) =>
        Mode == ShopMode.Selling ? Math.Min(MostAtOnce, inventory?.GetQuantity(item) ?? 0) : MostAffordable(item, money);

    /// <summary>
    /// Up and down the first question or the list; while "how many?" is up, left and right change the number by
    /// one and up and down by ten, wrapping between one and the most the money buys (or the bag holds). In the list
    /// a step that comes from a key kept down (<paramref name="held"/>) stops at either end instead of wrapping.
    /// </summary>
    public void Move(int dx, int dy, int money, bool held = false, Inventory? inventory = null)
    {
        if (dx == 0 && dy == 0) return;
        if (Mode == ShopMode.Choosing)
        {
            if (dy == 0) return;
            ChoiceIndex = UiNav.Wrap(ChoiceIndex, Math.Sign(dy), Choices.Length);
            AudioManager.PlaySound("cursor");
            return;
        }
        if (ChoosingQuantity)
        {
            int most = Math.Max(1, MostOf(Selected!, money, inventory));
            int next = Quantity + dx - dy * 10;
            // Past either end it wraps round; a jump of ten stops at the end first
            if (next > most) next = Quantity == most ? 1 : most;
            else if (next < 1) next = Quantity == 1 ? most : 1;
            if (next != Quantity) AudioManager.PlaySound("cursor");
            Quantity = next;
            return;
        }
        var list = Listed;
        if (dy == 0 || list.Count == 0) return;
        int row = held ? Math.Clamp(SelectedIndex + dy, 0, list.Count - 1) : UiNav.Wrap(SelectedIndex, dy, list.Count);
        if (held && row == SelectedIndex) return;
        SelectedIndex = row;
        FirstRow = UiNav.Window(FirstRow, SelectedIndex, list.Count, VisibleRows);
        if (tick.Sounds(held)) AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// The A button: on the first question, goes to the stock, to the bag's items or out; on an item, asks how many
    /// (or says the money doesn't reach); on the number, buys or sells them. Returns what the player's money
    /// changes by (less for a purchase, more for a sale).
    /// </summary>
    public int Confirm(Inventory inventory, int money, Action<string> onNotification)
    {
        if (Mode == ShopMode.Choosing)
        {
            switch (ChoiceIndex)
            {
                case 0:
                    Mode = ShopMode.Buying;
                    break;
                case 1:
                    Mode = ShopMode.Selling;
                    selling.Clear();
                    selling.AddRange(Sellable(inventory));
                    if (selling.Count == 0)
                    {
                        Mode = ShopMode.Choosing;
                        onNotification("You don't have anything we can buy from you.");
                        AudioManager.PlaySound("error");
                        return 0;
                    }
                    break;
                default:
                    Close();
                    AudioManager.PlaySound("cancel");
                    return 0;
            }
            SelectedIndex = 0;
            FirstRow = 0;
            AudioManager.PlaySound("select");
            return 0;
        }

        if (Selected is not { } item) return 0;
        if (!ChoosingQuantity)
        {
            if (Mode == ShopMode.Buying && MostAffordable(item, money) < 1)
            {
                onNotification("You don't have enough money.");
                AudioManager.PlaySound("error");
                return 0;
            }
            ChoosingQuantity = true;
            Quantity = 1;
            asking.Open();
            AudioManager.PlaySound("select");
            return 0;
        }

        int count = Math.Clamp(Quantity, 1, Math.Max(1, MostOf(item, money, inventory)));
        ChoosingQuantity = false;
        asking.Close();
        if (Mode == ShopMode.Selling)
        {
            if (!inventory.RemoveItem(item, count)) return 0;
            int earned = count * SellPrice(item);
            AudioManager.PlaySound("select");
            onNotification(count == 1 ? $"Sold a {item.Name} for {earned}." : $"Sold {count} × {item.Name} for {earned}.");
            selling.Clear();
            selling.AddRange(Sellable(inventory));
            SelectedIndex = Math.Min(SelectedIndex, Math.Max(0, selling.Count - 1));
            if (selling.Count == 0) Mode = ShopMode.Choosing;
            return earned;
        }

        int cost = count * item.Price;
        if (cost > money) return 0;
        inventory.AddItem(item, count);
        AudioManager.PlaySound("select");
        onNotification(count == 1 ? $"Bought a {item.Name}." : $"Bought {count} × {item.Name}.");
        return -cost;
    }

    /// <summary>The B button: out of "how many?", then back to the first question, then out of the shop.</summary>
    public void Cancel()
    {
        if (ChoosingQuantity)
        {
            ChoosingQuantity = false;
            asking.Close();
        }
        else if (Mode != ShopMode.Choosing)
        {
            Mode = ShopMode.Choosing;
            SelectedIndex = 0;
            FirstRow = 0;
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

        // Up or down kept down runs on through the list; the first question and "how many?" take presses only
        bool list = Mode != ShopMode.Choosing && !ChoosingQuantity;
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right);
        int dy = upDown.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down),
            list ? InputManager.Axis(GameAction.Up, GameAction.Down, held: true) : 0);
        // More than one step in a frame only ever comes from a key held in the list
        for (int i = 0; i < Math.Max(Math.Abs(dx), Math.Abs(dy)); i++) Move(dx, Math.Sign(dy), playerMoney, upDown.Repeating, playerInventory);

        // A button pressed while the list runs on still counts: it acts on the item the cursor has come to
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) playerMoney += Confirm(playerInventory, playerMoney, onNotification);
    }

    public void Draw(int screenWidth, int screenHeight, int playerMoney, Inventory inventory)
    {
        if (!IsActive) return;
        FirstRow = UiNav.Window(FirstRow, SelectedIndex, Listed.Count, VisibleRows);
        ModernUi.DrawShop(screenWidth, screenHeight, this, playerMoney, inventory, Math.Clamp(openAge / AppearTime, 0f, 1f), asking.Shown, asking.Visible);
    }
}
