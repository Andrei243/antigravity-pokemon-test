using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The Apricorn Box on screen (plan 20 · J6; style guide, "Apricorn trees").</summary>
internal static partial class ModernUi
{
    public static void DrawApricornBox(int sw, int sh, ApricornBoxScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("apricorn.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.move"), Hint("shared.keys.esc", "shared.hints.back"));
        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        const float listW = 860;
        var list = new Rectangle(Margin - slide, ContentTop, listW, ContentBottom - ContentTop);
        var detail = new Rectangle(Margin + listW + Gutter + slide, ContentTop, sw - Margin * 2 - listW - Gutter, ContentBottom - ContentTop);
        Panel(list, 34);
        Panel(detail, 34);
        var box = screen.Box;

        // The seven colours, each a row: its fruit on a disc, its name and how many
        for (int row = 0; row < ApricornBoxScreen.Colours.Length; row++)
        {
            var colour = ApricornBoxScreen.Colours[row];
            var r = RowRect(list, row, 24);
            bool selected = row == screen.Cursor;
            int count = box.Count(colour);
            var ink = ListRow(r, selected, ApricornData.NameOf(colour), enabled: count > 0, nameX: 148);
            ApricornFruit(new Vector2(r.X + 64, r.Y + r.Height / 2f), 26, colour, count > 0);
            RowValue(r, $"×{count}", ink);
        }

        // The one under the cursor: large, with the ball it becomes
        var chosen = screen.Selected;
        Label(Text.Of("apricorn.apricorn"), detail.X + 44, detail.Y + 30);
        string total = Text.Of("apricorn.inAll", ("n", box.Total));
        Tag(detail.X + detail.Width - 44 - UiFonts.Measure(total, 24, UiWeight.Black) - 34, detail.Y + 26, total, Frame, 40);
        var disc = new Vector2(detail.X + detail.Width / 2f, detail.Y + 230);
        UiShapes.Circle(disc, 130, Disc);
        UiShapes.Ring(disc, 130, 5, Rule);
        ApricornFruit(disc, 84, chosen, true);
        string name = ApricornData.NameOf(chosen);
        UiFonts.DrawCentered(name, detail.X + (detail.Width - UiFonts.Measure(name, 44, UiWeight.Black)) / 2f, detail.Y + 410, 44, Ink, UiWeight.Black);
        string has = Text.Of("apricorn.has", ("count", box.Count(chosen)), ("limit", ApricornBox.Limit));
        UiFonts.DrawCentered(has, detail.X + (detail.Width - UiFonts.Measure(has, 28, UiWeight.ExtraBold)) / 2f, detail.Y + 458, 28, Muted, UiWeight.ExtraBold);
        string makes = Text.Of("apricorn.makes", ("ball", ItemDatabase.Get(ApricornData.BallOf(chosen))?.DisplayName ?? ApricornData.BallOf(chosen)));
        UiFonts.DrawCentered(makes, detail.X + (detail.Width - UiFonts.Measure(makes, 30, UiWeight.ExtraBold)) / 2f, detail.Y + 518, 30, Ink, UiWeight.ExtraBold);

        // What Kurt is making
        float fy = detail.Y + detail.Height - 190;
        UiShapes.Fill(new Rectangle(detail.X + 44, fy, detail.Width - 88, 3), 1.5f, Rule);
        Label(Text.Of("apricorn.kurt"), detail.X + 44, fy + 22);
        string order = KurtsOrder(box);
        UiFonts.Draw(order, detail.X + 44, fy + 74, 28, order.Length > 0 && box.Order != null ? Ink : Muted, UiWeight.ExtraBold);
    }

    /// <summary>What Kurt has in hand, as a line: at work (the day he is to be done is the next), ready, or nothing.</summary>
    internal static string KurtsOrder(ApricornBox box)
    {
        if (box.Order is not { } order || ApricornData.Parse(order.Colour) is not { } colour) return Text.Of("apricorn.noOrder");
        string ball = ItemDatabase.Get(ApricornData.BallOf(colour))?.DisplayName ?? ApricornData.BallOf(colour);
        return Text.Of(box.Status(ApricornBox.DayOf(GameClock.Today)) == 2 ? "apricorn.orderReady" : "apricorn.orderWorking", ("count", order.Count), ("ball", ball));
    }

    /// <summary>An Apricorn drawn with shapes: a round fruit in its colour with a highlight, a shadow and a stalk (dimmed when none is held).</summary>
    private static void ApricornFruit(Vector2 at, float radius, Apricorn colour, bool held)
    {
        var body = ApricornArt.ColourOf(colour);
        if (!held) body = new Color((int)(body.R * 0.45f + 120), (int)(body.G * 0.45f + 120), (int)(body.B * 0.45f + 120), 255);
        var rim = new Color((int)(body.R * 0.55f), (int)(body.G * 0.55f), (int)(body.B * 0.55f), 255);
        UiShapes.Circle(at + new Vector2(0, radius * 0.05f), radius + 3, rim);
        UiShapes.Circle(at, radius, body);
        UiShapes.Circle(at + new Vector2(-radius * 0.35f, -radius * 0.4f), radius * 0.26f, new Color(255, 255, 255, 140));
        UiShapes.Fill(new Rectangle(at.X - radius * 0.07f, at.Y - radius * 1.25f, radius * 0.14f, radius * 0.4f), radius * 0.05f, new Color(96, 66, 44, 255));
    }
}
