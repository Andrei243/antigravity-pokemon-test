using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The packs screen under OPTIONS (plan 16 · T15; style guide, "Menu screens", Packs)
internal static partial class ModernUi
{
    public static void DrawPacks(PacksScreen screen, int sw, int sh)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("packScreen.title"));
        if (screen.Rows.Count > 0)
            Hints(sw - Margin, 44, Hint("shared.keys.leftRight", "packScreen.hint.order"), Hint("shared.keys.z", "packScreen.hint.toggle"), Hint("shared.keys.esc", "shared.hints.back"));
        else Hints(sw - Margin, 44, Hint("shared.keys.esc", "shared.hints.back"));

        // The list: a pack to a row, in the order they load, each with its version and whether it is on
        float listH = 24 + PacksScreen.VisibleRows * RowPitch + 12;
        var list = new Rectangle(Margin, ContentTop, sw - Margin * 2, listH);
        Panel(list, 30);
        bool scrolls = screen.Rows.Count > PacksScreen.VisibleRows;
        if (screen.Rows.Count == 0)
        {
            UiFonts.Draw(Text.Of("packScreen.none"), list.X + 52, list.Y + 48, 36, Ink, UiWeight.Black);
            float y = list.Y + 112;
            foreach (string folder in screen.Folders)
            {
                DrawWrapped(folder, list.X + 52, y, list.Width - 104, 26, Muted, 34, UiWeight.ExtraBold);
                y += 44;
            }
        }
        for (int k = 0; k < Math.Min(PacksScreen.VisibleRows, screen.Rows.Count); k++)
        {
            int i = screen.First + k;
            var row = screen.Rows[i];
            var r = RowRect(list, k, scrolls: scrolls);
            bool selected = i == screen.Selected;
            var ink = ListRow(r, selected, row.Pack.Name, row.Enabled, nameX: 76);
            UiFonts.DrawCentered((i + 1).ToString(), r.X + 28, r.Y + r.Height / 2f, 28, selected ? Color.White : Muted, UiWeight.Black);
            string state = Text.Of(row.Enabled ? "packScreen.on" : "packScreen.off");
            float stateW = UiFonts.Measure(state, 20, UiWeight.Black);
            float tagX = r.X + r.Width - 40 - stateW - 31;
            Tag(tagX, r.Y + (r.Height - 34) / 2f, state, row.Enabled ? Green : Muted, 34);
            string version = Text.Of("packScreen.version", ("version", row.Pack.Version));
            float vw = UiFonts.Measure(version, 28, UiWeight.ExtraBold);
            UiFonts.DrawCentered(version, tagX - 28 - vw, r.Y + r.Height / 2f, 28, ink, UiWeight.ExtraBold);
        }
        ScrollBar(list, screen.First, PacksScreen.VisibleRows, screen.Rows.Count);

        // The pack under the cursor: who made it, the credit it carries, what it is, and its place in the order
        float aboutTop = list.Y + list.Height + 16, noteH = 84;
        var about = new Rectangle(Margin, aboutTop, sw - Margin * 2, ContentBottom - aboutTop - noteH - 16);
        Panel(about, 30);
        if (screen.Current is { } current)
        {
            var pack = current.Pack;
            float x = about.X + 52, y = about.Y + 28;
            Label(Text.Of("packScreen.author"), x, y);
            UiFonts.Draw(pack.Author ?? Text.Of("packScreen.unknown"), x, y + 24, 34, Ink, UiWeight.Black);
            Label(Text.Of("packScreen.place"), about.X + about.Width - 380, y);
            Stepper(new Vector2(about.X + about.Width - 210, y + 66), screen.Place, 200, 64);
            Label(Text.Of("packScreen.credits"), x, y + 84);
            DrawWrapped(pack.Credits ?? "", x, y + 108, about.Width - 520, 28, Ink, 38, UiWeight.ExtraBold);
            if (pack.Description is { Length: > 0 } description)
            {
                Label(Text.Of("packScreen.about"), x, y + 210);
                DrawWrapped(description, x, y + 234, about.Width - 104, 28, Muted, 38, UiWeight.ExtraBold);
            }
            if (pack.After.Count > 0)
                UiFonts.Draw(Text.Of("packScreen.after", ("names", string.Join(", ", pack.After))), x, about.Y + about.Height - 52, 24, Muted, UiWeight.ExtraBold);
        }

        // When the change takes hold, or what couldn't be read
        var note = new Rectangle(Margin, ContentBottom - noteH, sw - Margin * 2, noteH);
        Panel(note, 30);
        (string text, Color color) = screen.Pending ? (Text.Of("packScreen.pending"), Red)
            : screen.Problems.Count > 0 ? (Text.Of("packScreen.problems", ("n", screen.Problems.Count), ("first", screen.Problems[0])), Red)
            : (Text.Of("packScreen.help"), Muted);
        DrawWrapped(text, note.X + 44, note.Y + (note.Height - 28) / 2f - 2, note.Width - 88, 28, color, 38, UiWeight.ExtraBold);
    }
}
