using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The title's HOME (plan 12 · Q28, Q29; style guide, "Menu screens", "Home").</summary>
internal static partial class ModernUi
{
    /// <summary>The room a side's panel keeps over its rows for its name.</summary>
    public const float HomeFooter = 64;

    public static void DrawHome(int sw, int sh, HomeScreen screen, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("homeBox.title"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        switch (screen.Phase)
        {
            case HomePhase.Menu:
                Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("shared.keys.z", "shared.hints.open"), Hint("shared.keys.esc", "shared.hints.back"));
                HomeMenu(sw, screen, rise);
                break;
            case HomePhase.ChooseSlot:
                Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("shared.keys.z", "shared.hints.open"), Hint("shared.keys.esc", "shared.hints.back"));
                HomeSlots(sw, screen, rise);
                break;
            default:
                Hints(sw - Margin, 44, Hint("shared.keys.arrows", "shared.hints.choose"), Hint("shared.keys.z", screen.Trading ? "shared.hints.pick" : "homeBox.hints.move"),
                    Hint("shared.keys.esc", "shared.hints.back"));
                HomeSides(sw, screen, rise);
                break;
        }

        if (screen.Phase == HomePhase.ConfirmTrade && screen.PickedOn(0) is { } mine && screen.PickedOn(1) is { } theirs)
            Prompt(sw, sh, Text.Of("homeBox.confirm.title"),
                Text.Of("homeBox.confirm.body", ("mine", mine.Made.DisplayName), ("theirs", theirs.Made.DisplayName),
                    ("left", screen.Left?.Save.PlayerName ?? ""), ("right", screen.Right?.Save.PlayerName ?? "")),
                new[] { (Text.Of("homeBox.confirm.no"), Blue), (Text.Of("homeBox.confirm.yes"), Green) }, screen.ConfirmYes ? 1 : 0);
        else if (screen.Phase == HomePhase.Working)
            Prompt(sw, sh, Text.Of("homeBox.working.title"), Text.Of("homeBox.working.body"), Array.Empty<(string, Color)>(), -1);
        else if (screen.Phase == HomePhase.Result)
        {
            if (screen.Done is { } done)
                Notice(sw, sh, Text.Of($"homeBox.done.{done}"), Text.Of("homeBox.done.body"), Text.Of("homeBox.ok"));
            else
                Notice(sw, sh, Text.Of("homeBox.refused"), HomeMoves.Words(screen.Outcome), Text.Of("homeBox.ok"));
        }
    }

    private static void HomeMenu(int sw, HomeScreen screen, float rise)
    {
        var list = new Rectangle(Margin, ContentTop + rise, 1040, ContentBottom - ContentTop);
        Panel(list, 40);
        string[] rows = { Text.Of("homeBox.menu.trade"), Text.Of("homeBox.menu.box") };
        for (int row = 0; row < rows.Length; row++)
            ListRow(RowRect(list, row), row == screen.MenuCursor, rows[row], enabled: row != 0 || screen.CanTrade, nameX: 44);

        var detail = new Rectangle(list.X + list.Width + Gutter, ContentTop + rise, sw - Margin * 2 - list.Width - Gutter, ContentBottom - ContentTop);
        Panel(detail, 40);
        float x = detail.X + 44, wide = detail.Width - 88, y = detail.Y + 48;
        UiFonts.Draw(rows[screen.MenuCursor], x, y, 40, Ink, UiWeight.Black);
        UiShapes.Fill(new Rectangle(x, y + 64, wide, 3), 1.5f, Rule);
        string body = screen.MenuCursor == 0
            ? Text.Of(screen.CanTrade ? "homeBox.menu.tradeBody" : "homeBox.menu.tradeNone")
            : Text.Of("homeBox.menu.boxBody", ("n", screen.Bank?.Count ?? 0), ("of", Models.HomeBank.Capacity));
        DrawWrapped(body, x, y + 96, wide, 30, Ink, 42);
        if (screen.MenuCursor == 1 && screen.BankRefusal != HomeRefusal.None)
            DrawWrapped(HomeMoves.Words(screen.BankRefusal), x, y + 300, wide, 28, Red, 40);
    }

    private static void HomeSlots(int sw, HomeScreen screen, float rise)
    {
        var list = new Rectangle(Margin, ContentTop + rise, 1040, ContentBottom - ContentTop);
        Panel(list, 40);
        var choices = screen.SlotChoices;
        for (int row = 0; row < choices.Count; row++)
        {
            var r = RowRect(list, row);
            var ink = ListRow(r, row == screen.SlotCursor, Fitted(choices[row].Save.PlayerName ?? "", r.Width - 104 - 240, 34), nameX: 44);
            RowValue(r, Text.Of("homeBox.slot", ("slot", choices[row].Number)), ink, 26);
        }
        var detail = new Rectangle(list.X + list.Width + Gutter, ContentTop + rise, sw - Margin * 2 - list.Width - Gutter, ContentBottom - ContentTop);
        Panel(detail, 40);
        float x = detail.X + 44, wide = detail.Width - 88, y = detail.Y + 48;
        string heading = !screen.Trading ? Text.Of("homeBox.choose.box") : screen.Left == null ? Text.Of("homeBox.choose.first") : Text.Of("homeBox.choose.second");
        DrawWrapped(heading, x, y, wide, 34, Ink, 46, UiWeight.Black);
        if (screen.Trading && screen.Left != null)
            DrawWrapped(Text.Of("homeBox.choose.with", ("name", screen.Left.Save.PlayerName ?? ""), ("slot", screen.Left.Number)), x, y + 120, wide, 28, Muted, 40);
    }

    private static void HomeSides(int sw, HomeScreen screen, float rise)
    {
        float width = (sw - Margin * 2 - Gutter) / 2f;
        for (int side = 0; side < 2; side++)
        {
            var panel = new Rectangle(Margin + side * (width + Gutter), ContentTop + rise, width, ContentBottom - ContentTop);
            bool active = side == screen.Side;
            Panel(panel, 40, frame: active ? Blue : null);
            string name = side == 0 || screen.Trading
                ? (side == 0 ? screen.Left : screen.Right) is { } slot ? Text.Of("homeBox.sideSave", ("name", slot.Save.PlayerName ?? ""), ("slot", slot.Number)) : ""
                : Text.Of("homeBox.sideHome", ("n", screen.Bank?.Count ?? 0), ("of", Models.HomeBank.Capacity));
            UiFonts.Draw(Fitted(name, panel.Width - 88, 32), panel.X + 44, panel.Y + 26, 32, active ? Blue : Ink, UiWeight.Black);

            var entries = screen.EntriesOf(side);
            int visible = HomeScreen.VisibleRows;
            if (entries.Count == 0)
            {
                EmptyNote(panel, Text.Of(side == 1 && !screen.Trading ? "homeBox.emptyHome" : "homeBox.empty"));
                continue;
            }
            bool scrolls = entries.Count > visible;
            int first = screen.FirstRowOf(side);
            for (int row = 0; row < visible && first + row < entries.Count; row++)
            {
                int index = first + row;
                var entry = entries[index];
                var r = RowRect(panel, row, 24 + HomeFooter, scrolls);
                bool selected = active && index == screen.CursorOf(side);
                bool chosen = screen.PickedOn(side) is { } picked && picked.Saved.Id == entry.Saved.Id;
                var made = entry.Made;
                var ink = ListRow(r, selected, Fitted(made.DisplayName, r.Width - 104 - 300, 34));
                // The one chosen on a side: a Gold edge round its row
                if (chosen)
                    UiShapes.Shape(new Rectangle(r.X - 6, r.Y - 6, r.Width + 12, r.Height + 12), 26, Color.Blank, Color.Blank, Gold, 6f);
                RowIcon(r, PixelArtGenerator.IconOf(made), 1f, selected, faded: made.IsEgg);
                string where = entry.OnTeam ? Text.Of("homeBox.team") : Text.Of("homeBox.box", ("n", entry.Box + 1));
                string value = Text.Of(entry.Randomised ? "homeBox.rowRandom" : "homeBox.row", ("where", where), ("level", made.Level));
                RowValue(r, value, ink, 24);
            }
            ScrollBar(panel, first, visible, entries.Count, 24 + HomeFooter);
        }
    }
}
