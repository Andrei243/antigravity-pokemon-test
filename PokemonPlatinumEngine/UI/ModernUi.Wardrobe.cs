using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The wardrobe and the boutique (plan 11 · C10): the player's clothes slot by slot beside their figure.</summary>
internal static partial class ModernUi
{
    /// <summary>A slot's name, sign and colour, in the order of <see cref="Outfit.Slots"/>.</summary>
    public static readonly (string Label, UiIcon Icon, Color Color)[] SlotTabs =
    {
        ("HATS", UiIcon.Hat, Red),
        ("TOPS", UiIcon.Shirt, Blue),
        ("BOTTOMS", UiIcon.Trousers, new Color(126, 110, 222, 255)),
        ("SHOES", UiIcon.Shoe, new Color(238, 134, 60, 255)),
        ("BAGS", UiIcon.Bag, Green)
    };

    private static readonly string[] OwnNames = { "Your Own Hat", "Your Own Top", "Your Own Bottoms", "Your Own Shoes", "Your Own Bag" };

    /// <summary>What a row of the wardrobe is called: the garment's name, or the look's own.</summary>
    public static string GarmentName(ClothingSlot slot, string? garment) =>
        ClothingDatabase.Get(garment)?.Name ?? OwnNames[(int)slot];

    /// <summary>
    /// The wardrobe: the slots as tabs, the player's figure on the left in what the cursor is on, the slot's clothes
    /// in the middle (each with a swatch of its colours and its price, or OWNED, or WORN), the money and the garment's
    /// card on the right, and "buy it?" over them once something is about to be bought.
    /// </summary>
    public static void DrawWardrobe(int sw, int sh, WardrobeScreen screen, int money, Texture2D? figure, float appear,
        float asking, bool askingVisible, float time)
    {
        Backdrop(sw, sh);
        ScreenTitle(screen.Name);
        bool forSale = screen.Selected is { } g && !screen.Owns(g.Id);
        Hints(sw - Margin, 44, ("Left / Right", "Slot"), ("Z", forSale ? "Buy" : "Wear"), ("Esc", "Done"));
        Tabs(new Rectangle(Margin, ContentTop, sw - Margin * 2, 76), SlotTabs, screen.Tab);

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        const float top = ContentTop + 76 + 24, figureW = 520, listW = 760;

        // ---- The player, in what the cursor is on
        var stage = new Rectangle(Margin - slide, top, figureW, ContentBottom - top);
        Panel(stage, 34);
        var floor = new Vector2(stage.X + stage.Width / 2f, stage.Y + stage.Height - 70);
        Raylib.DrawEllipse((int)floor.X, (int)floor.Y, 150, 30, Disc);
        if (figure is { } picture)
        {
            float h = stage.Height - 40, w = h * picture.Width / picture.Height;
            Raylib.DrawTexturePro(picture, new Rectangle(0, 0, picture.Width, -picture.Height),
                new Rectangle(floor.X - w / 2f, stage.Y + 16, w, h), Vector2.Zero, 0f, Color.White);
        }
        if (screen.Dressing)
        {
            // The outfit is being made: three dots that pulse in turn
            for (int i = 0; i < 3; i++)
            {
                float pulse = 0.5f + 0.5f * MathF.Sin(time * 6f - i * 0.9f);
                UiShapes.Circle(new Vector2(floor.X - 36 + i * 36, stage.Y + 56), 9 + pulse * 4, Muted with { A = (byte)(120 + pulse * 120) });
            }
        }

        // ---- The slot's clothes
        var list = new Rectangle(Margin + figureW + Gutter - slide, top, listW, ContentBottom - top);
        Panel(list, 34);
        var rows = screen.Rows;
        bool scrolls = rows.Count > WardrobeScreen.VisibleRows;
        for (int row = 0; row < WardrobeScreen.VisibleRows && screen.FirstRow + row < rows.Count; row++)
        {
            int index = screen.FirstRow + row;
            string? id = rows[index];
            var garment = ClothingDatabase.Get(id);
            var r = RowRect(list, row, 26, scrolls);
            bool selected = index == screen.SelectedIndex;
            var ink = ListRow(r, selected, GarmentName(screen.Slot, id));
            Swatch(new Vector2(r.X + 52, r.Y + r.Height / 2f), garment, selected);
            bool worn = string.Equals(screen.Worn[screen.Slot], id, StringComparison.OrdinalIgnoreCase);
            if (worn) RowTag(r, "WORN", selected ? Color.White : Green, selected ? Selection : Color.White);
            else if (screen.Owns(id)) RowValue(r, "OWNED", selected ? Color.White : Muted, 26);
            else MoneyRight(r.X + r.Width - 32, r.Y + r.Height / 2f, garment!.Price, 30, selected ? Color.White : garment.Price <= money ? ink : Red);
        }
        ScrollBar(list, screen.FirstRow, WardrobeScreen.VisibleRows, rows.Count, 26);

        // ---- The money, and the garment under the cursor
        float rx = list.X + listW + Gutter + slide * 2, rw = sw - Margin - rx;
        var purse = new Rectangle(rx, top, rw, 112);
        Panel(purse, 34);
        UiFonts.DrawCentered("MONEY", purse.X + 36, purse.Y + purse.Height / 2f, 24, Muted, UiWeight.Black);
        MoneyRight(purse.X + purse.Width - 36, purse.Y + purse.Height / 2f, money, 44, Ink);

        var card = new Rectangle(rx, top + 112 + 24, rw, ContentBottom - top - 112 - 24);
        Panel(card, 34);
        GarmentCard(card, screen, money);

        if (askingVisible && screen.Selected is { } buying) BuyPrompt(sw, sh, buying, screen.AskIndex, asking);
    }

    /// <summary>A garment's colours on the row's disc: its main colour with its second as a band; a cross for none at all.</summary>
    private static void Swatch(Vector2 c, Garment? garment, bool selected)
    {
        UiShapes.Circle(c, 34, selected ? Color.White : Disc);
        if (garment == null)
        {
            // The look's own: a small star
            Star(c, 18, Gold);
            return;
        }
        if (garment.IsNothing)
        {
            UiShapes.Ring(c, 20, 5, Muted);
            UiShapes.Line(c + new Vector2(-14, 14), c + new Vector2(14, -14), 5, Muted);
            return;
        }
        UiShapes.Circle(c, 24, ColorOf(garment.Color!));
        if (garment.Accent != null)
            UiShapes.Ring(c, 24, 7, ColorOf(garment.Accent));
        UiShapes.Ring(c, 25, 2, Frame with { A = 90 });
    }

    /// <summary>A small filled tag at the row's right end.</summary>
    private static void RowTag(Rectangle r, string text, Color fill, Color ink)
    {
        const float h = 40, size = 22;
        float w = UiFonts.Measure(text, size, UiWeight.Black) + 34;
        var t = new Rectangle(r.X + r.Width - 28 - w, r.Y + (r.Height - h) / 2f, w, h);
        UiShapes.Fill(t, h / 2f, fill);
        UiFonts.DrawCentered(text, t.X + 17, t.Y + h / 2f, size, ink, UiWeight.Black);
    }

    /// <summary>The garment under the cursor: its name, its slot, what it costs or that it is owned, and its line.</summary>
    private static void GarmentCard(Rectangle card, WardrobeScreen screen, int money)
    {
        var garment = screen.Selected;
        string? id = garment?.Id;
        float x = card.X + 36, room = card.Width - 72;
        string name = GarmentName(screen.Slot, id);
        float size = 40, nameW = UiFonts.Measure(name, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        UiFonts.DrawCentered(name, x, card.Y + 64, size, Ink, UiWeight.Black);
        var tab = SlotTabs[screen.Tab];
        float tx = x + Tag(x, card.Y + 100, tab.Label, tab.Color, 38) + 12;
        bool worn = string.Equals(screen.Worn[screen.Slot], id, StringComparison.OrdinalIgnoreCase);
        if (worn) Tag(tx, card.Y + 100, "WORN", Green, 38);
        else if (screen.Owns(id)) Tag(tx, card.Y + 100, "OWNED", Muted, 38);

        if (garment is { IsFree: false } && !screen.Owns(id))
        {
            Label("PRICE", x, card.Y + 166);
            Money(x, card.Y + 214, garment.Price, 40, garment.Price <= money ? Ink : Red);
        }

        float y = card.Y + 266;
        UiShapes.Fill(new Rectangle(x, y, room, 3), 1.5f, Rule);
        y += 28;
        string text = garment?.Description is { Length: > 0 } d ? d
            : "What you set out in. You can always change back into it.";
        foreach (var line in Wrap(text, room, 28))
        {
            UiFonts.Draw(line, x, y, 28, Ink);
            y += 40;
        }
    }

    /// <summary>"Buy it?" over the dimmed wardrobe: the garment, its price and BUY or NO.</summary>
    private static void BuyPrompt(int sw, int sh, Garment garment, int cursor, float appear)
    {
        Dim(sw, sh, (int)(150 * appear));
        var r = new Rectangle(sw / 2f - 520, sh / 2f - 180 + (1f - appear) * 60f, 1040, 360);
        Panel(r, 36);
        UiFonts.Draw($"Buy the {garment.Name}?", r.X + 56, r.Y + 44, 48, Ink, UiWeight.Black);
        float w = Money(r.X + 56, r.Y + 142, garment.Price, 40, Ink);
        UiFonts.DrawCentered("Yours for good, to wear at home too.", r.X + 56 + w + 16, r.Y + 142, 28, Muted, UiWeight.ExtraBold);
        const float gap = 32;
        float bw = (r.Width - 112 - gap) / 2f;
        for (int i = 0; i < WardrobeScreen.BuyChoices.Length; i++)
            Button(new Rectangle(r.X + 56 + i * (bw + gap), r.Y + 226, bw, 84), 42, i == 0 ? Green : Blue, WardrobeScreen.BuyChoices[i], 32, i == cursor);
    }

    private static Color ColorOf(string hex)
    {
        var (r, g, b) = Garment.Rgb(hex);
        return new Color(r, g, b, (byte)255);
    }
}
