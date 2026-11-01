using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The trainer tips (plan 12 · Q23; style guide, "Field menus and notices", "A tip", and "Menu screens", Help's TIPS)
internal static partial class ModernUi
{
    /// <summary>The tip card's size, and how far its middle stands above the screen's.</summary>
    public const float TipWidth = 1040, TipHeight = 360, TipLift = 40;

    /// <summary>
    /// A tip over the dimmed field or battle: the star on a Gold chip, TIP over the title, the lines under them and the
    /// confirm key's hint at the bottom right. <paramref name="appear"/> rises it into place.
    /// </summary>
    public static void TipCard(int sw, int sh, string title, string body, float appear = 1f)
    {
        float eased = UiMotion.EaseOut(Math.Clamp(appear, 0f, 1f));
        Dim(sw, sh, (int)(110 * eased));
        var r = new Rectangle(sw / 2f - TipWidth / 2f, sh / 2f - TipLift - TipHeight / 2f + (1f - eased) * 60f, TipWidth, TipHeight);
        Panel(r, 36);
        var chip = new Vector2(r.X + 56, r.Y + 60);
        UiShapes.Circle(chip, 26, Gold);
        UiIcons.Draw(UiIcon.Star, chip, 13, Color.White, Gold);
        UiFonts.Draw(Text.Of("tips.caption"), r.X + 100, r.Y + 30, 20, Muted, UiWeight.Black);
        UiFonts.Draw(title, r.X + 100, r.Y + 54, 44, Ink, UiWeight.Black);
        DrawWrapped(body, r.X + 56, r.Y + 140, r.Width - 112, 30, Ink, 42, UiWeight.ExtraBold);
        Hints(r.X + r.Width - 40, r.Y + r.Height - 76, Hint("shared.keys.z", "tips.ok"));
    }

    /// <summary>Help's TIPS page: the tips shown, a list at the left and the one under the cursor at the right.</summary>
    private static void TipsPanel(HelpScreen screen, float top, int sw)
    {
        var seen = screen.TipsSeen;
        if (seen.Count == 0)
        {
            var panel = new Rectangle(Margin, top, sw - Margin * 2, ContentBottom - top);
            Panel(panel, 30);
            string none = Text.Of("tips.none");
            float w = UiFonts.Measure(none, 30, UiWeight.ExtraBold);
            UiFonts.DrawCentered(none, panel.X + (panel.Width - w) / 2f, panel.Y + panel.Height / 2f, 30, Muted, UiWeight.ExtraBold);
            return;
        }
        var list = new Rectangle(Margin, top, 640, ContentBottom - top);
        Panel(list, 30);
        bool scrolls = seen.Count > HelpScreen.VisibleTipRows;
        for (int row = 0; row < HelpScreen.VisibleTipRows && screen.FirstTip + row < seen.Count; row++)
        {
            int i = screen.FirstTip + row;
            ListRow(RowRect(list, row, scrolls: scrolls), !screen.OnTabs && i == screen.TipCursor, seen[i].Title, nameX: 32, size: 30);
        }
        ScrollBar(list, screen.FirstTip, HelpScreen.VisibleTipRows, seen.Count);

        float x = list.X + list.Width + Gutter;
        var side = new Rectangle(x, top, sw - Margin - x, ContentBottom - top);
        Panel(side, 30);
        var tip = seen[Math.Clamp(screen.TipCursor, 0, seen.Count - 1)];
        var chip = new Vector2(side.X + 70, side.Y + 74);
        UiShapes.Circle(chip, 26, Gold);
        UiIcons.Draw(UiIcon.Star, chip, 13, Color.White, Gold);
        UiFonts.Draw(Text.Of("tips.caption"), side.X + 114, side.Y + 44, 20, Muted, UiWeight.Black);
        UiFonts.Draw(tip.Title, side.X + 114, side.Y + 68, 44, Ink, UiWeight.Black);
        DrawWrapped(tip.Body, side.X + 44, side.Y + 160, side.Width - 88, 30, Ink, 42, UiWeight.ExtraBold);
    }
}
