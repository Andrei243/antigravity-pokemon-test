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
                // (a folder is one word to the wrapping, so one too long for the panel ran out past it: shortened in
                // its middle instead, its drive and its last folders kept)
                UiFonts.Draw(Shortened(folder, list.Width - 104, 26, UiWeight.ExtraBold), list.X + 52, y, 26, Muted, UiWeight.ExtraBold);
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
            // The parts follow one another down the panel: a long credit pushes WHAT IT IS down, and the description
            // stops a line above "Loads after …" (both were written at fixed places and ran over one another)
            float bottom = about.Y + about.Height - (pack.After.Count > 0 ? 64 : 24);
            var credit = Wrap(pack.Credits ?? "", about.Width - 520, 28, UiWeight.ExtraBold);
            float at = y + 108;
            foreach (string line in credit)
            {
                if (at + 38 > bottom) break;
                if (line.Length > 0) UiFonts.Draw(line, x, at, 28, Ink, UiWeight.ExtraBold);
                at += 38;
            }
            if (pack.Description is { Length: > 0 } description)
            {
                float top = Math.Max(y + 210, at + 26);
                if (top + 24 + 38 <= bottom)
                {
                    Label(Text.Of("packScreen.about"), x, top);
                    at = top + 24;
                    foreach (string line in Wrap(description, about.Width - 104, 28, UiWeight.ExtraBold))
                    {
                        if (at + 38 > bottom) break;
                        if (line.Length > 0) UiFonts.Draw(line, x, at, 28, Muted, UiWeight.ExtraBold);
                        at += 38;
                    }
                }
            }
            if (pack.After.Count > 0)
                UiFonts.Draw(Text.Of("packScreen.after", ("names", string.Join(", ", pack.After))), x, about.Y + about.Height - 52, 24, Muted, UiWeight.ExtraBold);
        }

        // When the change takes hold, or what couldn't be read
        var note = new Rectangle(Margin, ContentBottom - noteH, sw - Margin * 2, noteH);
        Panel(note, 30);
        // Both when both hold: a pack left out was hidden while a change waited (the screen opened with a pack that
        // couldn't be read said only that the changes wait)
        var lines = new System.Collections.Generic.List<string>();
        if (screen.Pending) lines.Add(Text.Of("packScreen.pending"));
        if (screen.Problems.Count > 0)
            lines.Add(Text.Of("packScreen.problems", ("n", screen.Problems.Count), ("first", screen.Problems[0])));
        var color = lines.Count > 0 ? Red : Muted;
        if (lines.Count == 0) lines.Add(Text.Of("packScreen.help"));
        float pitch = lines.Count > 1 ? 32 : 38, size = lines.Count > 1 ? 24 : 28;
        float lineY = note.Y + (note.Height - pitch * (lines.Count - 1) - size) / 2f - 2;
        foreach (string line in lines)
        {
            UiFonts.Draw(Shortened(line, note.Width - 88, size, UiWeight.ExtraBold), note.X + 44, lineY, size, color, UiWeight.ExtraBold);
            lineY += pitch;
        }
    }

    /// <summary>A line cut in its middle with an ellipsis until it fits the width, its start and its end kept.</summary>
    private static string Shortened(string text, float width, float size, UiWeight weight)
    {
        if (UiFonts.Measure(text, size, weight) <= width) return text;
        int keep = text.Length - 1;
        while (keep > 2)
        {
            int head = keep / 3, tail = keep - head;
            string cut = text[..head] + "…" + text[^tail..];
            if (UiFonts.Measure(cut, size, weight) <= width) return cut;
            keep--;
        }
        return "…";
    }
}
