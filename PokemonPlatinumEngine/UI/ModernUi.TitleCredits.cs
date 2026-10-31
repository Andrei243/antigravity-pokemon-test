using System;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The title's credits (plan 16 · T1; style guide, "Menu screens", "Credits")
internal static partial class ModernUi
{
    public static void DrawTitleCredits(int sw, int sh, TitleCreditsScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("titleCredits.title"));
        Hints(sw - Margin, 44,
            Hint("shared.keys.upDown", "titleCredits.hint.scroll"),
            Hint("shared.keys.leftRight", "titleCredits.hint.section"),
            Hint("shared.keys.esc", "shared.hints.back"));

        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var panel = new Rectangle(Margin, ContentTop + rise, sw - Margin * 2, ContentBottom - ContentTop);
        Panel(panel, 34);

        var rows = screen.Rows;
        float x = panel.X + 44, y = panel.Y + 24;
        int visible = TitleCreditsScreen.VisibleRows;
        for (int i = 0; i < visible && screen.First + i < rows.Count; i++)
        {
            var row = rows[screen.First + i];
            float cy = y + i * TitleCreditsScreen.RowPitch + TitleCreditsScreen.RowPitch / 2f;
            switch (row.Kind)
            {
                case CreditsRowKind.Heading:
                    // The first section, the project's own, in Blue; the records' headings in Ink
                    UiFonts.DrawCentered(row.Text, x, cy, TitleCreditsScreen.HeadingSize, row.Section == 0 ? Blue : Ink, UiWeight.Black);
                    break;
                case CreditsRowKind.Text:
                    UiFonts.DrawCentered(row.Text, x, cy, TitleCreditsScreen.TextSize, Ink, UiWeight.ExtraBold);
                    break;
            }
        }
        // The lists' track and thumb, but over this panel's own height: its rows are not a list's
        if (rows.Count > visible)
        {
            var track = new Rectangle(panel.X + panel.Width - 26, panel.Y + 24, 10, panel.Height - 48);
            UiShapes.Fill(track, 5, Rule);
            float size = MathF.Max(40f, track.Height * visible / rows.Count);
            float at = (track.Height - size) * screen.First / MathF.Max(1, screen.LastFirst);
            UiShapes.Fill(new Rectangle(track.X, track.Y + at, track.Width, size), 5, Frame);
        }
    }
}
