using System;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The first start (plan 16 · T24; style guide, "Opening and title screen", "The first start")
internal static partial class ModernUi
{
    public static void DrawFirstStart(int sw, int sh, FirstStartScreen screen, GameSettings settings)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("firstStart.title"));
        if (screen.OnContinue) Hints(sw - Margin, 44, Hint("shared.keys.z", "firstStart.hint.continue"));
        else Hints(sw - Margin, 44, Hint("shared.keys.leftRight", "shared.hints.change"), Hint("shared.keys.z", "shared.hints.change"));

        float slide = (1f - UiMotion.EaseOut(screen.Appear)) * 60f;
        // The page's own line under the title, in white on the backdrop
        DrawWrapped(Text.Of("firstStart.line"), Margin, ContentTop + slide, sw - Margin * 2, 30, Color.White, 40, UiWeight.ExtraBold);

        const float pitch = 86, height = 78;
        float top = ContentTop + 96 + slide;
        int count = screen.Rows.Count + 1;
        int shown = Math.Min(FirstStartScreen.VisibleRows, count);
        bool scrolls = count > FirstStartScreen.VisibleRows;
        float width = sw - Margin * 2 - (scrolls ? 36 : 0);
        for (int k = 0; k < shown; k++)
        {
            int i = screen.First + k;
            var r = new Rectangle(Margin, top + k * pitch, width, height);
            bool selected = i == screen.Cursor;
            if (i < screen.Rows.Count)
            {
                var (label, value, _) = FirstStartScreen.Describe(settings, screen.Rows[i]);
                ValueRow(r, label, value, selected);
            }
            else
            {
                // CONTINUE: a card of its own in Green, its word centred
                Card(r, 30, selected, Green);
                string word = Text.Of("firstStart.continue.label");
                UiFonts.DrawCentered(word, r.X + (r.Width - UiFonts.Measure(word, 36, UiWeight.Black)) / 2f, r.Y + r.Height / 2f, 36, Ink, UiWeight.Black);
            }
        }
        if (scrolls)
        {
            var track = new Rectangle(sw - Margin - 12, top, 12, shown * pitch - (pitch - height));
            UiShapes.Fill(track, 6, Rule);
            float size = track.Height * shown / count;
            float at = (track.Height - size) * screen.First / (count - shown);
            UiShapes.Fill(new Rectangle(track.X, track.Y + at, track.Width, size), 6, Frame);
        }

        // What the row under the cursor does, as the options say it, or what CONTINUE does
        float helpTop = top + shown * pitch + 6;
        var help = new Rectangle(Margin, helpTop, sw - Margin * 2, Math.Max(120, ContentBottom - helpTop));
        Panel(help, 30);
        string text = screen.Selected is { } row ? FirstStartScreen.Describe(settings, row).Help : Text.Of("firstStart.continue.help");
        DrawWrapped(text, help.X + 52, help.Y + (help.Height - 30) / 2f - 2, help.Width - 104, 30, Ink, 40);
    }
}
