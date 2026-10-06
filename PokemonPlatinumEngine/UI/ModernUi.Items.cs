using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The screens that list items: the bag and a shop's counter.</summary>
internal static partial class ModernUi
{
    /// <summary>A pocket's name, sign and colour, in the order of <see cref="BagScreen.Pockets"/>.</summary>
    public static readonly (string Label, UiIcon Icon, Color Color)[] PocketTabs =
    {
        ("ITEMS", UiIcon.Pouch, Gold),
        ("MEDICINE", UiIcon.Cross, new Color(238, 108, 140, 255)),
        ("POKÉ BALLS", UiIcon.Ball, Red),
        ("TMs & HMs", UiIcon.Disc, new Color(126, 110, 222, 255)),
        ("BERRIES", UiIcon.Berry, Green),
        ("MAIL", UiIcon.Letter, new Color(84, 168, 216, 255)),
        ("BATTLE ITEMS", UiIcon.Boost, new Color(238, 134, 60, 255)),
        ("KEY ITEMS", UiIcon.Key, new Color(150, 104, 200, 255))
    };

    public static (string Label, UiIcon Icon, Color Color) PocketTab(ItemPocket pocket) =>
        PocketTabs[Math.Max(0, Array.IndexOf(BagScreen.Pockets, pocket))];

    /// <summary>
    /// The bag: the pockets as tabs, the pocket's items as a list and the chosen one on the right, with what
    /// can be done with it on a small panel while the A button's menu is up.
    /// </summary>
    public static void DrawBag(int sw, int sh, BagScreen bag, Inventory inventory, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("BAG");
        Hints(sw - Margin, 44, ("Left / Right", "Pocket"), ("Z", "Choose"), ("Esc", "Back"));

        int pocket = Math.Max(0, Array.IndexOf(BagScreen.Pockets, bag.CurrentPocket));
        Tabs(new Rectangle(Margin, ContentTop, sw - Margin * 2, 76), PocketTabs, pocket);

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        var items = inventory.GetPocketItems(bag.CurrentPocket);
        const float top = ContentTop + 76 + 24;

        // ---- The pocket
        var list = new Rectangle(Margin - slide, top, 920, ContentBottom - top);
        Panel(list, 34);
        bool scrolls = items.Count > BagScreen.VisibleRows;
        if (items.Count == 0) EmptyNote(list, "Nothing in this pocket.");
        for (int row = 0; row < BagScreen.VisibleRows && bag.FirstRow + row < items.Count; row++)
        {
            int index = bag.FirstRow + row;
            var stack = items[index];
            var r = RowRect(list, row, 26, scrolls);
            bool selected = index == bag.SelectedIndex;
            var ink = ListRow(r, selected, stack.Name);
            RowIcon(r, PixelArtGenerator.GetItemIcon(stack.Data), 1.5f, selected);
            if (bag.CurrentPocket != ItemPocket.KeyItems) RowValue(r, $"×{stack.Quantity}", ink);
        }
        ScrollBar(list, bag.FirstRow, BagScreen.VisibleRows, items.Count, 26);

        // ---- The chosen item
        var detail = new Rectangle(Margin + 920 + Gutter + slide, top, sw - Margin * 2 - 920 - Gutter, ContentBottom - top);
        Panel(detail, 34);
        if (items.Count == 0 || bag.SelectedIndex >= items.Count) return;
        var item = items[bag.SelectedIndex].Data;
        ItemDetail(detail, item, PocketTab(item.Pocket), item.Pocket == ItemPocket.KeyItems ? null : ("IN BAG", $"×{items[bag.SelectedIndex].Quantity}"));

        if (bag.Actions is { } actions) ActionMenu(detail, actions, bag.ActionIndex);
    }

    /// <summary>An item large on its disc with its name, a tag, one caption and value, and its text; a TM's move under it.</summary>
    private static void ItemDetail(Rectangle detail, ItemData item, (string Label, UiIcon Icon, Color Color) tab, (string Caption, string Value)? fact)
    {
        ItemDisc(new Vector2(detail.X + 44 + 96, detail.Y + 44 + 96), item);
        float x = detail.X + 44 + 192 + 36;
        float size = 48, room = detail.X + detail.Width - 44 - x;
        float nameW = UiFonts.Measure(item.Name, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        UiFonts.DrawCentered(item.Name, x, detail.Y + 84, size, Ink, UiWeight.Black);
        Tag(x, detail.Y + 124, tab.Label, tab.Color, 38);
        if (fact is { } f)
        {
            Label(f.Caption, x, detail.Y + 186);
            UiFonts.Draw(f.Value, x, detail.Y + 208, 36, Ink, UiWeight.Black);
        }

        // A TM or an HM is described by the move it teaches, as in the games
        var move = string.IsNullOrEmpty(item.TeachesMove) ? null : MoveDatabase.Get(item.TeachesMove);
        string text = move != null && !string.IsNullOrWhiteSpace(move.Description) ? move.Description : item.Description;

        float y = detail.Y + 276;
        UiShapes.Fill(new Rectangle(detail.X + 44, y, detail.Width - 88, 3), 1.5f, Rule);
        y += 30;
        var lines = Wrap(text, detail.Width - 88, 30);
        foreach (var line in lines)
        {
            UiFonts.Draw(line, detail.X + 44, y, 30, Ink);
            y += 42;
        }

        if (move == null) return;
        y += 22;
        UiShapes.Fill(new Rectangle(detail.X + 44, y, detail.Width - 88, 3), 1.5f, Rule);
        y += 26;
        Label("TEACHES", detail.X + 44, y);
        UiFonts.Draw(move.Name, detail.X + 44, y + 24, 40, Ink, UiWeight.Black);
        float px = detail.X + 44;
        px += TypePill(px, y + 84, move.Type, 42) + 12;
        Tag(px, y + 84, move.Category.ToString().ToUpperInvariant(), Muted, 42);
        float fx = detail.X + 44;
        fx = Field("POWER", move.Power > 0 ? move.Power.ToString() : "—", fx, y + 148, 36);
        fx = Field("ACCURACY", move.Accuracy > 0 ? move.Accuracy.ToString() : "—", fx, y + 148, 36);
        Field("PP", move.MaxPP.ToString(), fx, y + 148, 36);
    }

    private static string ActionLabel(BagAction action) => action switch
    {
        BagAction.Use => "USE",
        BagAction.Give => "GIVE",
        BagAction.Register => "REGISTER",
        BagAction.Deselect => "DESELECT",
        _ => "CANCEL"
    };

    /// <summary>What can be done with the chosen item: a small panel at the foot of the detail panel, one row for each.</summary>
    private static void ActionMenu(Rectangle detail, IReadOnlyList<BagAction> actions, int selected)
    {
        const float width = 360, row = 74;
        float height = 20 + actions.Count * row + 12;
        var panel = new Rectangle(detail.X + detail.Width - width - 28, detail.Y + detail.Height - height - 28, width, height);
        Panel(panel, 30);
        for (int i = 0; i < actions.Count; i++)
        {
            var r = new Rectangle(panel.X + 14, panel.Y + 16 + i * row, width - 28, row - 8);
            ListRow(r, i == selected, ActionLabel(actions[i]), nameX: 40, size: 32);
        }
    }

    /// <summary>
    /// A shop's counter: the stock with its prices, the player's money, the chosen item, and over them "how
    /// many?" once something is being bought.
    /// </summary>
    public static void DrawShop(int sw, int sh, ShopScreen shop, int money, Inventory inventory, float appear, float asking, bool askingVisible)
    {
        Backdrop(sw, sh);
        ScreenTitle(shop.Name);
        Hints(sw - Margin, 44, ("Z", "Buy"), ("Esc", "Leave"));

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        var stock = shop.Stock;

        // ---- The stock
        var list = new Rectangle(Margin - slide, ContentTop, 920, ContentBottom - ContentTop);
        Panel(list, 34);
        bool scrolls = stock.Count > ShopScreen.VisibleRows;
        for (int row = 0; row < ShopScreen.VisibleRows && shop.FirstRow + row < stock.Count; row++)
        {
            int index = shop.FirstRow + row;
            var item = stock[index];
            var r = RowRect(list, row, 40, scrolls);
            bool selected = index == shop.SelectedIndex;
            bool affordable = item.Price <= money;
            var ink = ListRow(r, selected, item.Name);
            RowIcon(r, PixelArtGenerator.GetItemIcon(item), 1.5f, selected);
            MoneyRight(r.X + r.Width - 32, r.Y + r.Height / 2f, item.Price, 30, selected ? Color.White : affordable ? ink : Red);
        }
        ScrollBar(list, shop.FirstRow, ShopScreen.VisibleRows, stock.Count, 40);

        // ---- The player's money
        float rx = Margin + 920 + Gutter + slide, rw = sw - Margin * 2 - 920 - Gutter;
        var purse = new Rectangle(rx, ContentTop, rw, 132);
        Panel(purse, 34);
        UiFonts.DrawCentered("MONEY", purse.X + 44, purse.Y + purse.Height / 2f, 26, Muted, UiWeight.Black);
        MoneyRight(purse.X + purse.Width - 44, purse.Y + purse.Height / 2f, money, 52, Ink);

        // ---- The chosen item
        var detail = new Rectangle(rx, ContentTop + 132 + 24, rw, ContentBottom - ContentTop - 132 - 24);
        Panel(detail, 34);
        if (shop.Selected is { } chosen)
            ItemDetail(detail, chosen, PocketTab(chosen.Pocket), ("IN BAG", $"×{inventory.GetQuantity(chosen)}"));

        if (askingVisible && shop.Selected is { } buying) QuantityPrompt(sw, sh, buying, shop.Quantity, asking);
    }

    /// <summary>"Buy how many?" over the dimmed shop: the item, a stepper and what that many cost.</summary>
    private static void QuantityPrompt(int sw, int sh, ItemData item, int quantity, float appear)
    {
        Dim(sw, sh, (int)(150 * appear));
        var r = new Rectangle(sw / 2f - 480, sh / 2f - 220 + (1f - appear) * 60f, 960, 440);
        Panel(r, 36);
        UiFonts.Draw("Buy how many?", r.X + 56, r.Y + 44, 48, Ink, UiWeight.Black);
        HintsDark(r.X + r.Width - 40, r.Y + 46, ("Z", "Buy"), ("Esc", "Back"));

        var c = new Vector2(r.X + 56 + 52, r.Y + 176);
        UiShapes.Circle(c, 52, Disc);
        PixelArt(PixelArtGenerator.GetItemIcon(item), c, 2);
        UiFonts.DrawCentered(item.Name, r.X + 56 + 128, r.Y + 160, 40, Ink, UiWeight.Black);
        float each = Money(r.X + 56 + 128, r.Y + 204, item.Price, 26, Muted);
        UiFonts.DrawCentered("each", r.X + 56 + 128 + each + 10, r.Y + 204, 24, Muted, UiWeight.ExtraBold);

        Stepper(new Vector2(r.X + r.Width - 56 - 44 - 120 - 24, r.Y + 180), $"× {quantity}");

        UiShapes.Fill(new Rectangle(r.X + 56, r.Y + 272, r.Width - 112, 3), 1.5f, Rule);
        UiFonts.DrawCentered("TOTAL", r.X + 56, r.Y + 350, 28, Muted, UiWeight.Black);
        MoneyRight(r.X + r.Width - 56, r.Y + 350, item.Price * quantity, 60, Ink);
    }
}
