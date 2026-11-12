using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The salon and the dye house (plan 43 · LF3; style guide, "Menu screens", "Wardrobe"): the wardrobe's frame, its tabs the salon's.</summary>
internal static partial class ModernUi
{
    /// <summary>The salon's tabs: CUT, COLOUR, EYES and SKIN, each with its sign and colour.</summary>
    public static (string Label, UiIcon Icon, Color Color)[] SalonTabs => new[]
    {
        (Text.Of("salon.tab.Cut"), UiIcon.Hat, Red),
        (Text.Of("salon.tab.Colour"), UiIcon.Star, new Color(126, 110, 222, 255)),
        (Text.Of("salon.tab.Eyes"), UiIcon.Disc, Blue),
        (Text.Of("salon.tab.Skin"), UiIcon.Trainer, new Color(238, 134, 60, 255))
    };

    /// <summary>
    /// The salon or the dye house: the tabs (the salon's four; the dye house's step as one tab), the player's figure on
    /// the left in what the cursor is on, the list in the middle (a swatch of each colour, WORN on what is worn), the
    /// money and the price on the right, and "do it?" over them once a change is about to be paid.
    /// </summary>
    public static void DrawSalon(int sw, int sh, SalonScreen screen, int money, Texture2D? figure, float appear, float asking, bool askingVisible)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of(screen.Dyeing ? "salon.dyeHouse" : "salon.title"));
        Hints(sw - Margin, 44, screen.Dyeing ? Hint("shared.keys.upDown", "shared.hints.choose") : Hint("shared.keys.leftRight", "shared.hints.change"),
            Hint("shared.keys.z", "shared.hints.choose"), Hint("shared.keys.esc", "shared.hints.done"));
        if (screen.Dyeing)
            Tabs(new Rectangle(Margin, ContentTop, sw - Margin * 2, 76), new[]
            {
                (Text.Of("salon.step.Garment"), UiIcon.Shirt, Blue),
                (Text.Of("salon.step.Dye"), UiIcon.Berry, Red)
            }, (int)screen.Step);
        else Tabs(new Rectangle(Margin, ContentTop, sw - Margin * 2, 76), SalonTabs, (int)screen.Tab);

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        const float top = ContentTop + 76 + 24, figureW = 520, listW = 760;

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

        var list = new Rectangle(Margin + figureW + Gutter - slide, top, listW, ContentBottom - top);
        Panel(list, 34);
        var rows = screen.Rows;
        bool scrolls = rows.Count > SalonScreen.VisibleRows;
        if (rows.Count == 0)
            UiFonts.Draw(Text.Of("salon.nothingToDye"), list.X + 36, list.Y + 48, 30, Muted, UiWeight.Black);
        for (int row = 0; row < SalonScreen.VisibleRows && screen.FirstRow + row < rows.Count; row++)
        {
            int index = screen.FirstRow + row;
            string? id = rows[index];
            var r = RowRect(list, row, 26, scrolls);
            bool selected = index == screen.SelectedIndex;
            ListRow(r, selected, screen.NameOf(id));
            var c = new Vector2(r.X + 52, r.Y + r.Height / 2f);
            UiShapes.Circle(c, 34, selected ? Color.White : Disc);
            if (screen.SwatchOf(id) is { } swatch)
            {
                UiShapes.Circle(c, 24, swatch);
                UiShapes.Ring(c, 25, 2, Frame with { A = 90 });
            }
            else Star(c, 18, Gold);
            bool worn = !screen.Dyeing && string.Equals(WornPart(screen), id, StringComparison.OrdinalIgnoreCase);
            if (worn) RowTag(r, Text.Of("outfits.worn"), selected ? Color.White : Green, selected ? Selection : Color.White);
        }
        ScrollBar(list, screen.FirstRow, SalonScreen.VisibleRows, rows.Count, 26);

        float rx = list.X + listW + Gutter + slide * 2, rw = sw - Margin - rx;
        var purse = new Rectangle(rx, top, rw, 112);
        Panel(purse, 34);
        UiFonts.DrawCentered(Text.Of("outfits.money"), purse.X + 36, purse.Y + purse.Height / 2f, 24, Muted, UiWeight.Black);
        MoneyRight(purse.X + purse.Width - 36, purse.Y + purse.Height / 2f, money, 44, Ink);
        var card = new Rectangle(rx, top + 112 + 24, rw, ContentBottom - top - 112 - 24);
        Panel(card, 34);
        float x = card.X + 36;
        Label(Text.Of("outfits.price"), x, card.Y + 40);
        Money(x, card.Y + 88, screen.Price, 40, screen.Price <= money ? Ink : Red);
        if (screen.Dyeing && screen.Step == DyeStep.Dye && ClothingDatabase.DyeOf(screen.Selected) is { } dye)
        {
            Label(Text.Of("salon.withBerry"), x, card.Y + 150);
            UiFonts.Draw(LocalNames.Item(dye.Berry), x, card.Y + 178, 32, Ink, UiWeight.Black);
        }
        float y = card.Y + 250;
        UiShapes.Fill(new Rectangle(x, y, card.Width - 72, 3), 1.5f, Rule);
        y += 28;
        foreach (var line in Wrap(Text.Of(screen.Dyeing ? "salon.dyeNote" : "salon.note"), card.Width - 72, 28))
        {
            UiFonts.Draw(line, x, y, 28, Ink);
            y += 40;
        }

        if (askingVisible)
        {
            Dim(sw, sh, (int)(150 * asking));
            var r = new Rectangle(sw / 2f - 520, sh / 2f - 180 + (1f - asking) * 60f, 1040, 360);
            Panel(r, 36);
            UiFonts.Draw(Text.Of(screen.Dyeing ? "salon.dyeQuestion" : "salon.question", ("what", screen.NameOf(screen.Selected))), r.X + 56, r.Y + 44, 44, Ink, UiWeight.Black);
            Money(r.X + 56, r.Y + 142, screen.Price, 40, Ink);
            const float gap = 32;
            float bw = (r.Width - 112 - gap) / 2f;
            string[] answers = { Text.Of("salon.answer.yes"), Text.Of("outfits.answer.no") };
            for (int i = 0; i < 2; i++)
                Button(new Rectangle(r.X + 56 + i * (bw + gap), r.Y + 226, bw, 84), 42, i == 0 ? Green : Blue, answers[i], 32, i == screen.AskIndex);
        }
    }

    private static string? WornPart(SalonScreen screen) => screen.Tab switch
    {
        SalonTab.Cut => screen.Worn.Hair,
        SalonTab.Colour => screen.Worn.HairColour,
        SalonTab.Eyes => screen.Worn.Eyes,
        _ => screen.Worn.Skin
    };
}
