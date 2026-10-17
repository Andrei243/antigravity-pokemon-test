using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The Controls page under OPTIONS (plan 12 · Q5; style guide, "Menu screens", Controls)
internal static partial class ModernUi
{
    private const float ControlsPitch = 62, ControlsRow = 56;

    public static void DrawControls(ControlsScreen screen, int sw, int sh)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("controls.page.title"));
        ControlsHints(screen, sw);

        // The list: a header of the two columns, then an action to a row, the reset row last
        const float headerH = 44;
        float listH = 16 + headerH + ControlsScreen.VisibleRows * ControlsPitch + 12;
        var list = new Rectangle(Margin, ContentTop, sw - Margin * 2, listH);
        Panel(list, 30);
        float nameX = list.X + 44, keysX = list.X + 520, padX = list.X + 1180, cellW = 600;
        Label(Text.Of("controls.page.keyboard"), keysX + 8, list.Y + 14);
        Label(Text.Of("controls.page.pad"), padX + 8, list.Y + 14);

        float top = list.Y + 16 + headerH;
        bool scrolls = ControlsScreen.RowCount > ControlsScreen.VisibleRows;
        for (int k = 0; k < Math.Min(ControlsScreen.VisibleRows, ControlsScreen.RowCount); k++)
        {
            int i = screen.First + k;
            var r = new Rectangle(list.X + 16, top + k * ControlsPitch, list.Width - 32 - (scrolls ? 26 : 0), ControlsRow);
            bool selected = i == screen.Selected;
            if (i == ControlsScreen.ResetRow)
            {
                ListRow(r, selected, Text.Of("controls.page.resetAll"), nameX: 28, size: 30);
                continue;
            }

            var action = ControlsScreen.Actions[i];
            bool clash = screen.ClashesOf(action).Any();
            if (selected) UiShapes.Fill(r, 22, Selection with { A = 46 });
            if (screen.IsChanged(action)) UiShapes.Circle(new Vector2(r.X + 18, r.Y + r.Height / 2f), 6, Blue);
            UiFonts.DrawCentered(Text.Of($"controls.action.{action}"), nameX, r.Y + r.Height / 2f, 30, clash ? Red : Ink, UiWeight.Black);

            var inputs = screen.Working[action];
            ControlsCell(new Rectangle(keysX, r.Y + 4, cellW, ControlsRow - 8), selected && screen.Column == ControlsColumn.Keyboard, screen,
                inputs.Keys.Select(KeyOfInput).ToArray(), null, action, ControlsColumn.Keyboard);
            ControlsCell(new Rectangle(padX, r.Y + 4, cellW - 40, ControlsRow - 8), selected && screen.Column == ControlsColumn.Pad, screen,
                null, inputs.Pad, action, ControlsColumn.Pad);
        }
        if (scrolls)
        {
            var track = new Rectangle(list.X + list.Width - 28, top, 10, ControlsScreen.VisibleRows * ControlsPitch - (ControlsPitch - ControlsRow));
            UiShapes.Fill(track, 5, Rule);
            float size = Math.Max(40f, track.Height * ControlsScreen.VisibleRows / ControlsScreen.RowCount);
            float at = (track.Height - size) * screen.First / Math.Max(1, ControlsScreen.RowCount - ControlsScreen.VisibleRows);
            UiShapes.Fill(new Rectangle(track.X, track.Y + at, track.Width, size), 5, Frame);
        }

        // What is happening, in words
        float noteTop = list.Y + list.Height + 16;
        var note = new Rectangle(Margin, noteTop, sw - Margin * 2, ContentBottom - noteTop);
        Panel(note, 30);
        var (text, color) = ControlsNote(screen);
        DrawWrapped(text, note.X + 44, note.Y + (note.Height - 30) / 2f - 2, note.Width - 88, 30, color, 40, UiWeight.ExtraBold);
    }

    private static string KeyOfInput(KeyboardKey key) => InputManager.KeyName(key);

    private static void ControlsHints(ControlsScreen screen, int sw)
    {
        switch (screen.Phase)
        {
            case ControlsPhase.Capturing:
                Hints(sw - Margin, 44, Hint("shared.keys.esc", "controls.page.hint.cancel"));
                break;
            case ControlsPhase.Choosing:
                Hints(sw - Margin, 44, Hint("shared.keys.z", "controls.page.hint.swap"), Hint("shared.keys.x", "controls.page.hint.keep"));
                break;
            case ControlsPhase.ResettingAll:
                Hints(sw - Margin, 44, Hint("shared.keys.z", "controls.page.hint.yes"), Hint("shared.keys.x", "controls.page.hint.no"));
                break;
            default:
                Hints(sw - Margin, 44, Hint("shared.keys.leftRight", "controls.page.hint.column"), Hint("shared.keys.z", "controls.page.hint.change"),
                    Hint("shared.keys.c", "controls.page.hint.reset"), Hint("shared.keys.esc", "shared.hints.back"));
                break;
        }
    }

    private static (string Text, Color Color) ControlsNote(ControlsScreen screen)
    {
        string action = screen.SelectedAction is { } a ? Text.Of($"controls.action.{a}") : "";
        switch (screen.Phase)
        {
            case ControlsPhase.Capturing:
                return (Text.Of(screen.Column == ControlsColumn.Keyboard ? "controls.page.pressKey" : "controls.page.pressButton", ("action", action)), Ink);
            case ControlsPhase.Choosing when screen.Pending is { } p:
                return (Text.Of("controls.page.choose", ("input", p.Input.Name), ("others", string.Join(", ", p.Holders.Select(h => Text.Of($"controls.action.{h}"))))), Red);
            case ControlsPhase.ResettingAll:
                return (Text.Of("controls.page.resetAsk"), Ink);
        }
        if (screen.Notice.Length > 0) return (Text.Of(screen.Notice, ("action", action)), Ink);
        if (screen.SelectedAction is { } sel && screen.ClashesOf(sel).ToArray() is { Length: > 0 } clashes)
            return (Text.Of("controls.page.clash", ("others", string.Join(", ", clashes.Select(h => Text.Of($"controls.action.{h}"))))), Red);
        return (Text.Of("controls.page.help"), Muted);
    }

    /// <summary>One action's keys or buttons as pills from the cell's left; the chosen cell is ringed, and waiting for a press it says so.</summary>
    private static void ControlsCell(Rectangle cell, bool chosen, ControlsScreen screen, string[]? keys, GamepadButton[]? pad, GameAction action, ControlsColumn column)
    {
        bool waiting = chosen && screen.Phase == ControlsPhase.Capturing;
        if (chosen) UiShapes.Shape(cell, cell.Height / 2f, default, default, waiting ? Red : Selection, 4);
        float cy = cell.Y + cell.Height / 2f, x = cell.X + 16;
        if (waiting)
        {
            // The place of the keys says the page is listening
            UiFonts.DrawCentered(Text.Of(column == ControlsColumn.Keyboard ? "controls.page.waitKey" : "controls.page.waitButton"), x, cy, 26, Red, UiWeight.Black);
            return;
        }
        if (keys != null)
        {
            if (keys.Length == 0) UiFonts.DrawCentered(Text.Of("controls.unbound"), x, cy, 24, Muted, UiWeight.ExtraBold);
            foreach (string key in keys)
            {
                float w = UiFonts.Measure(key, 22, UiWeight.Black) + 32;
                UiShapes.Fill(new Rectangle(x, cy - 20, w, 40), 20, Frame);
                UiFonts.DrawCentered(key, x + 16, cy, 22, Color.White, UiWeight.Black);
                x += w + 10;
            }
        }
        else if (pad != null)
        {
            if (pad.Length == 0) UiFonts.DrawCentered(Text.Of("controls.unbound"), x, cy, 24, Muted, UiWeight.ExtraBold);
            foreach (var button in pad)
            {
                if (PadGlyphs.Of(button) is { } glyph)
                {
                    float w = PadGlyphArt.Width(glyph);
                    PadGlyphArt.Draw(glyph, new Vector2(x + w / 2f, cy), 0.9f);
                    x += w * 0.9f + 18;
                }
                else
                {
                    string name = InputManager.PadName(button);
                    float w = UiFonts.Measure(name, 22, UiWeight.Black) + 32;
                    UiShapes.Fill(new Rectangle(x, cy - 20, w, 40), 20, Blue);
                    UiFonts.DrawCentered(name, x + 16, cy, 22, Color.White, UiWeight.Black);
                    x += w + 10;
                }
            }
        }
    }
}
