using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The Tales on screen (plan 46 · CP11; style guide, "Menu screens", "The Book of Tales", and "Battle panels", "The TALE
// button"): the Book's pages, a painted chest's colours on the Journal's page, and the move menu's TALE button beside the
// item button, its tale's name and its key.
internal static partial class ModernUi
{
    /// <summary>The Book's colours: a painted chest's red and its ochre (our own, from the style guide).</summary>
    private static readonly Color TaleRed = new(168, 44, 48, 255), TaleOchre = new(222, 164, 64, 255), TaleDeep = new(96, 26, 34, 255);

    /// <summary>A page of the Book of Tales: the tale's name, what it does when told, and its story on the rules.</summary>
    public static void DrawBookOfTales(int sw, int sh, BookOfTalesScreen screen, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("bookOfTales.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.leftRight", "shared.hints.turnPage"), Hint("shared.keys.esc", "shared.hints.close"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var page = new Rectangle(sw / 2f - 760, ContentTop + 6 + rise, 1520, ContentBottom - ContentTop - 6);
        float textX = page.X + 196, firstRule = page.Y + 300;
        NotebookPage(page, firstRule, page.X + 168);
        if (screen.Current is not { } id)
        {
            EmptyNote(page, Text.Of("bookOfTales.empty"));
            return;
        }

        // ---- A band of the chest's red over the page's head, the tale's name on it, and an ochre rule under it
        var band = new Rectangle(page.X + 168, page.Y + 40, page.Width - 228, 96);
        UiShapes.Shape(band, 14, TaleRed, TaleDeep, Darker(TaleDeep, 0.3f), 2);
        for (int i = 0; i < 9; i++)
        {
            var c = new Vector2(band.X + band.Width - 40 - i * 34, band.Y + band.Height / 2f);
            UiShapes.Circle(c, 6, i % 2 == 0 ? TaleOchre : Lighter(TaleRed, 0.25f));
        }
        string name = TaleTable.NameOf(id).ToUpperInvariant();
        UiFonts.DrawCentered(name, band.X + 32, band.Y + band.Height / 2f, 52, Color.White, UiWeight.Black);
        UiShapes.Line(new Vector2(band.X, band.Y + band.Height + 12), new Vector2(band.X + band.Width, band.Y + band.Height + 12), 4, TaleOchre);

        // ---- What it does when told, under the band
        UiFonts.Draw(Text.Of("bookOfTales.when").ToUpperInvariant(), textX, page.Y + 168, 22, Muted, UiWeight.Black);
        float y = page.Y + 200;
        foreach (string part in Wrap(TaleTable.RuleOf(id), page.X + page.Width - 60 - textX, 30, UiWeight.ExtraBold))
        {
            UiFonts.Draw(part, textX, y, 30, TaleDeep, UiWeight.ExtraBold);
            y += 38;
        }

        // ---- Its story, each line on a rule
        int row = 0;
        float lastRule = page.Y + page.Height - 70;
        foreach (string part in Wrap(TaleTable.StoryOf(id), page.X + page.Width - 60 - textX, 32, UiWeight.ExtraBold))
        {
            float rule = firstRule + (row + 1) * NotebookRule;
            if (rule >= lastRule) break;
            UiFonts.DrawCentered(part, textX, rule - 24, 32, Ink, UiWeight.ExtraBold);
            row++;
        }

        // ---- Which page, between arrows where there are more
        int count = screen.Tales.Count;
        string where = $"{screen.Page + 1} / {count}";
        float cx = page.X + page.Width / 2f + 84, cy = page.Y + page.Height - 40;
        UiFonts.DrawCentered(where, cx - UiFonts.Measure(where, 28, UiWeight.Black) / 2f, cy, 28, Muted, UiWeight.Black);
        if (screen.Page > 0) UiIcons.ArrowH(new Vector2(cx - 90, cy), 18, -1, Selection);
        if (screen.Page < count - 1) UiIcons.ArrowH(new Vector2(cx + 90, cy), 18, 1, Selection);
    }

    /// <summary>Where the move menu's TALE button stands (GPU-free: the screen's own state): null where no tale can be told.</summary>
    public static (string Tale, bool Armed)? TaleButtonOf(BattleEngine battle) =>
        battle.CanTellTaleNow && battle.TaleOffered is { } tale ? (tale, battle.TaleArmed != null) : null;

    /// <summary>
    /// The move menu's TALE button (style guide, "Battle panels", "The TALE button"): over the field like the item button,
    /// above it, an open book's mark, the tale it would tell and its key; lit in the chest's red while armed.
    /// </summary>
    private static void TaleButton(BattleEngine battle)
    {
        if (TaleButtonOf(battle) is not var (tale, armed)) return;
        string word = TaleTable.NameOf(tale).ToUpperInvariant();
        string key = Text.Of("shared.keys.pageRight");
        float wordWidth = UiFonts.Measure(word, 24, UiWeight.Black);
        float keyWidth = Math.Max(34f, UiFonts.Measure(key, 22, UiWeight.Black) + 18f);
        float width = 72 + wordWidth + 18 + keyWidth + 14;
        // In the item button's place where it has none, over it where it has one; its right edge where the item button's is
        float top = LaterButtonOf(battle).Mark == LaterMark.None ? 772 : 700;
        var r = new Rectangle(1216 - width, top, width, 62);
        if (armed)
        {
            UiShapes.Shadow(r, 31, 24, Vector2.Zero, TaleOchre with { A = 170 });
            UiShapes.Shape(r, 31, TaleRed, TaleDeep, Darker(TaleDeep, 0.3f), 3f);
        }
        else
        {
            UiShapes.Shadow(r, 31, 14, new Vector2(0, 6), ShadowColor);
            UiShapes.Shape(r, 31, PanelTop, PanelBottom, Frame, 3f);
        }
        var disc = new Vector2(r.X + 37, r.Y + 31);
        UiShapes.Circle(disc, 25, armed ? Color.White : new Color(232, 236, 246, 255));
        // An open book: two pages from a spine, an ochre clasp
        UiShapes.Triangle(disc + new Vector2(0, -10), disc + new Vector2(-17, -6), disc + new Vector2(-17, 12), TaleRed);
        UiShapes.Triangle(disc + new Vector2(0, -10), disc + new Vector2(-17, 12), disc + new Vector2(0, 8), TaleRed);
        UiShapes.Triangle(disc + new Vector2(0, -10), disc + new Vector2(17, -6), disc + new Vector2(17, 12), TaleDeep);
        UiShapes.Triangle(disc + new Vector2(0, -10), disc + new Vector2(17, 12), disc + new Vector2(0, 8), TaleDeep);
        UiShapes.Circle(disc + new Vector2(0, 0), 3.5f, TaleOchre);
        UiFonts.DrawCentered(word, r.X + 72, r.Y + 31, 24, armed ? Color.White : Ink, UiWeight.Black);
        var pill = new Rectangle(r.X + r.Width - 14 - keyWidth, r.Y + 14, keyWidth, 34);
        UiShapes.Shape(pill, 17, armed ? Darker(TaleDeep, 0.2f) : Frame, armed ? Darker(TaleDeep, 0.35f) : Darker(Frame, 0.15f));
        float kw = UiFonts.Measure(key, 22, UiWeight.Black);
        UiFonts.DrawCentered(key, pill.X + (pill.Width - kw) / 2f, pill.Y + 17, 22, Color.White, UiWeight.Black);
    }
}
