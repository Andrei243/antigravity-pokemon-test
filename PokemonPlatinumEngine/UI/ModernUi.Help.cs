using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The help pages under OPTIONS (plan 12 · Q10; style guide, "Menu screens", Help)
internal static partial class ModernUi
{
    private static readonly (string Label, UiIcon Icon, Color Color)[] HelpTabs =
    {
        ("TYPES", UiIcon.Boost, new Color(232, 72, 76, 255)),
        ("CONTROLS", UiIcon.Key, new Color(70, 136, 232, 255)),
        ("NOTES", UiIcon.Letter, new Color(56, 182, 104, 255))
    };

    public static void DrawHelp(HelpScreen screen, int sw, int sh)
    {
        Backdrop(sw, sh);
        ScreenTitle("HELP");
        if (screen.Page == HelpPage.Types && !screen.OnTabs) Hints(sw - Margin, 44, ("Arrows", "Look"), ("Esc", "Back"));
        else Hints(sw - Margin, 44, ("Left / Right", "Page"), ("Esc", "Back"));

        var strip = new Rectangle(Margin, ContentTop, sw - Margin * 2, 76);
        // The tabs glow while the cursor is on them
        if (screen.OnTabs) UiShapes.Shadow(strip, 38, 18, Vector2.Zero, Selection with { A = 90 });
        Tabs(strip, HelpTabs, (int)screen.Page);

        const float top = ContentTop + 76 + 24;
        switch (screen.Page)
        {
            case HelpPage.Types:
                TypeChartPanel(screen, top, sw);
                break;
            case HelpPage.Controls:
                ControlsPanel(top, sw);
                break;
            default:
                NotesPanel(top, sw);
                break;
        }
    }

    /// <summary>A cell's signed pill: the hint's sign and ×2, ×½ or ×0 on its colour, or a faint dot for a plain matchup.</summary>
    public static void ChartPill(Rectangle r, MoveHint hint)
    {
        var c = new Vector2(r.X + r.Width / 2f, r.Y + r.Height / 2f);
        if (MoveHints.Words(hint) == null)
        {
            UiShapes.Circle(c, 3, Rule);
            return;
        }
        var color = HintColor(hint);
        UiShapes.Shape(r, r.Height / 2f, Lighter(color, 0.1f), Darker(color, 0.08f));
        string text = hint switch { MoveHint.SuperEffective => "×2", MoveHint.NotVeryEffective => "×½", _ => "×0" };
        float w = UiFonts.Measure(text, 16, UiWeight.Black);
        UiFonts.DrawCentered(text, c.X - w / 2f, c.Y, 16, Color.White, UiWeight.Black);
    }

    private static void TypeChartPanel(HelpScreen screen, float top, int sw)
    {
        const float pitchX = 44, pitchY = 40, labelW = 150;
        var types = HelpScreen.Types;
        var chart = new Rectangle(Margin, top, 24 + labelW + 12 + types.Length * pitchX + 24, ContentBottom - top);
        Panel(chart, 30);
        float gridX = chart.X + 24 + labelW + 12, gridY = chart.Y + 20 + pitchY;
        bool cursor = !screen.OnTabs;

        // The cursor's row and column lit faintly, under everything
        if (cursor)
        {
            var lit = Selection with { A = 34 };
            UiShapes.Fill(new Rectangle(chart.X + 16, gridY + screen.Attack * pitchY, chart.Width - 32, pitchY), 14, lit);
            UiShapes.Fill(new Rectangle(gridX + screen.Defend * pitchX, chart.Y + 14, pitchX, chart.Height - 28), 14, lit);
        }

        for (int d = 0; d < types.Length; d++)
        {
            var color = Palette.GetTypeColor(types[d].ToString());
            var head = new Rectangle(gridX + d * pitchX + 2, chart.Y + 20 + 6, pitchX - 4, 28);
            UiShapes.Shape(head, 10, Lighter(color, 0.12f), Darker(color, 0.1f));
            string name = HelpScreen.ShortName(types[d]);
            float w = UiFonts.Measure(name, 13, UiWeight.Black);
            UiFonts.DrawCentered(name, head.X + (head.Width - w) / 2f, head.Y + head.Height / 2f, 13, Color.White, UiWeight.Black);
        }
        for (int a = 0; a < types.Length; a++)
        {
            float y = gridY + a * pitchY;
            TypePill(chart.X + 24, y + 4, types[a], pitchY - 8, labelW);
            for (int d = 0; d < types.Length; d++)
                ChartPill(new Rectangle(gridX + d * pitchX + 2, y + 5, pitchX - 4, pitchY - 10), HelpScreen.Hint(types[a], types[d]));
        }
        if (cursor)
        {
            var cell = new Rectangle(gridX + screen.Defend * pitchX - 1, gridY + screen.Attack * pitchY + 1, pitchX + 2, pitchY - 2);
            UiShapes.Shape(cell, 16, default, default, Selection, 4);
        }

        // What the cursor is on, in words, and what the pills mean
        float x = chart.X + chart.Width + Gutter;
        var side = new Rectangle(x, top, sw - Margin - x, ContentBottom - top);
        Panel(side, 30);
        float px = side.X + 44, py = side.Y + 44;
        Label("ATTACKING", px, py);
        Label("DEFENDING", px + 250, py);
        TypePill(px, py + 32, types[screen.Attack], 44, 200);
        TypePill(px + 250, py + 32, types[screen.Defend], 44, 200);
        var hint = HelpScreen.Hint(types[screen.Attack], types[screen.Defend]);
        var (headline, line) = screen.Describe();
        float hy = py + 136;
        if (MoveHints.Words(hint) != null) HintSign(hint, new Vector2(px + 18, hy), 14, HintColor(hint));
        UiFonts.DrawCentered(headline, px + (MoveHints.Words(hint) != null ? 48 : 0), hy, 44, Ink, UiWeight.Black);
        DrawWrapped(line, px, hy + 44, side.Width - 88, 30, Muted, 40, UiWeight.ExtraBold);

        float ly = side.Y + side.Height - 44 - 4 * 64;
        UiShapes.Fill(new Rectangle(px, ly - 28, side.Width - 88, 3), 1.5f, Rule);
        (MoveHint Hint, string Text)[] legend =
        {
            (MoveHint.SuperEffective, "Twice the damage"),
            (MoveHint.NotVeryEffective, "Half the damage"),
            (MoveHint.NoEffect, "No damage at all")
        };
        for (int i = 0; i < legend.Length; i++)
        {
            ChartPill(new Rectangle(px, ly + i * 64, 64, 34), legend[i].Hint);
            UiFonts.DrawCentered(legend[i].Text, px + 88, ly + i * 64 + 17, 28, Ink, UiWeight.ExtraBold);
        }
        DrawWrapped("Against two types, both count: ×2 and ×2 make four times, ×2 and ×½ cancel out.",
            px, ly + 3 * 64 - 6, side.Width - 88, 26, Muted, 34, UiWeight.ExtraBold);
    }

    private static void ControlsPanel(float top, int sw)
    {
        var panel = new Rectangle(Margin, top, sw - Margin * 2, ContentBottom - top);
        Panel(panel, 30);
        var rows = InputManager.Bindings.Select(b => (b.Name, Keys: b.Keys.Select(InputManager.KeyName).ToArray(), Pad: (string?)InputManager.PadName(b.Pad)))
            .Concat(HelpScreen.OtherKeys.Select(k => (k.Name, Keys: new[] { k.Key }, Pad: (string?)null))).ToArray();
        int perColumn = (rows.Length + 1) / 2;
        float colW = (panel.Width - 64 - Gutter) / 2f, pitch = (panel.Height - 48) / perColumn;
        for (int i = 0; i < rows.Length; i++)
        {
            var r = new Rectangle(panel.X + 32 + (i / perColumn) * (colW + Gutter), panel.Y + 24 + (i % perColumn) * pitch, colW, pitch - 8);
            if (i % perColumn > 0) UiShapes.Fill(new Rectangle(r.X + 16, r.Y - 5, r.Width - 32, 2), 1, Rule);
            UiFonts.DrawCentered(rows[i].Name, r.X + 24, r.Y + r.Height / 2f, 30, Ink, UiWeight.Black);
            float kx = r.X + r.Width - 16;
            if (rows[i].Pad is { } pad) kx -= KeyPill(kx, r.Y + r.Height / 2f, pad, Blue) + 16;
            foreach (string key in rows[i].Keys.Reverse()) kx -= KeyPill(kx, r.Y + r.Height / 2f, key, Frame) + 10;
        }
    }

    /// <summary>A key's name on a pill ending at <paramref name="rightX"/>; returns its width.</summary>
    private static float KeyPill(float rightX, float centerY, string text, Color color)
    {
        float w = UiFonts.Measure(text, 22, UiWeight.Black) + 32;
        UiShapes.Fill(new Rectangle(rightX - w, centerY - 20, w, 40), 20, color);
        UiFonts.DrawCentered(text, rightX - w + 16, centerY, 22, Color.White, UiWeight.Black);
        return w;
    }

    private static void NotesPanel(float top, int sw)
    {
        var panel = new Rectangle(Margin, top, sw - Margin * 2, ContentBottom - top);
        Panel(panel, 30);
        float colW = (panel.Width - 112 - Gutter * 2) / 2f;
        for (int i = 0; i < HelpScreen.Notes.Length; i++)
        {
            var (title, text) = HelpScreen.Notes[i];
            float x = panel.X + 56 + (i % 2) * (colW + Gutter * 2), y = panel.Y + 48 + (i / 2) * (panel.Height - 96) / 2f;
            UiFonts.Draw(title, x, y, 40, Ink, UiWeight.Black);
            DrawWrapped(text, x, y + 64, colW, 30, Ink, 42);
        }
    }
}
