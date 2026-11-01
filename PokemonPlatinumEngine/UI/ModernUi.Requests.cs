using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The requests page, the start menu's chip for it and the pinned line over the field (plan 12 · Q21; style guide,
// "Menu screens", "Requests", and "Field menus and notices", "Start menu" and "A request pinned")
internal static partial class ModernUi
{
    /// <summary>The colour of REQUESTS: its chip in the start menu and the OPEN tab.</summary>
    public static readonly Color RequestColour = new(204, 84, 160, 255);

    private static (string Label, UiIcon Icon, Color Color)[] RequestTabs(RequestsScreen screen)
    {
        var all = new[]
        {
            (Text.Of("requests.tab.open"), UiIcon.Letter, RequestColour),
            (Text.Of("requests.tab.done"), UiIcon.Star, Green),
            (Text.Of("requests.tab.area"), UiIcon.MapCard, Blue)
        };
        return screen.HasAreaTab ? all : all[..2];
    }

    public static void DrawRequests(int sw, int sh, RequestsScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("requests.title"));
        var keys = new System.Collections.Generic.List<HintItem> { Hint("shared.keys.leftRight", "requests.hint.tab") };
        if (screen.Tab == RequestsTab.Open && screen.Selected is { } under)
            keys.Add(Hint("shared.keys.z", screen.Pinned == under.Id ? "requests.hint.unpin" : "requests.hint.pin"));
        keys.Add(Hint("shared.keys.esc", "shared.hints.back"));
        Hints(sw - Margin, 44, keys.ToArray());
        var tabs = RequestTabs(screen);
        int tabIndex = Math.Max(0, Array.IndexOf(System.Linq.Enumerable.ToArray(screen.Tabs), screen.Tab));
        Tabs(new Rectangle(Margin, ContentTop, sw - Margin * 2, 76), tabs, tabIndex);

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        const float top = ContentTop + 76 + 24;
        if (screen.Tab == RequestsTab.Area)
        {
            AreaTab(new Rectangle(Margin, top + slide, sw - Margin * 2, ContentBottom - top), screen);
            return;
        }

        // ---- The list
        var list = new Rectangle(Margin - slide, top, 1000, ContentBottom - top);
        Panel(list, 34);
        var entries = screen.Entries;
        if (entries.Count == 0)
        {
            EmptyNote(list, Text.Of(screen.Tab == RequestsTab.Open ? "requests.noneOpen" : "requests.noneDone"));
            return;
        }
        bool scrolls = entries.Count > RequestsScreen.VisibleRows;
        for (int row = 0; row < RequestsScreen.VisibleRows && screen.First + row < entries.Count; row++)
        {
            int index = screen.First + row;
            var request = entries[index];
            var r = RowRect(list, row, 24, scrolls);
            bool selected = index == screen.Cursor;
            ListRow(r, selected, request.Title, true, 52, 32);
            if (screen.Pinned == request.Id)
            {
                var sign = new Vector2(r.X + r.Width - 48, r.Y + r.Height / 2f);
                UiShapes.Circle(sign, 20, selected ? Color.White : Gold);
                UiIcons.Draw(UiIcon.Pin, sign, 11, selected ? Gold : Color.White, selected ? Color.White : Gold);
            }
            else if (screen.Tab == RequestsTab.Done)
                RowValue(r, Text.Of("requests.doneTag"), selected ? Color.White : Green, 24);
        }
        ScrollBar(list, screen.First, RequestsScreen.VisibleRows, entries.Count, 24);

        // ---- The one under the cursor
        var detail = new Rectangle(Margin + 1000 + Gutter + slide, top, sw - Margin * 2 - 1000 - Gutter, ContentBottom - top);
        Panel(detail, 34);
        if (screen.Selected is { } chosen) RequestDetail(detail, chosen, screen);
    }

    private static void RequestDetail(Rectangle detail, Request request, RequestsScreen screen)
    {
        float x = detail.X + 44, room = detail.Width - 88;
        string name = request.Title;
        float size = 44, nameW = UiFonts.Measure(name, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        UiFonts.DrawCentered(name, x, detail.Y + 66, size, Ink, UiWeight.Black);

        float y = detail.Y + 104;
        string from = Text.Of("requests.from", ("giver", request.GiverName), ("place", request.PlaceName));
        var fromLines = Wrap(from, room, 26, UiWeight.ExtraBold);
        DrawWrapped(from, x, y, room, 26, Muted, 34, UiWeight.ExtraBold);
        y += fromLines.Count * 34 + 12;
        bool done = Requests.State(request, screen.Story) == RequestState.Done;
        if (done) Tag(x, y, Text.Of("requests.doneTag"), Green, 38);
        else if (screen.Pinned == request.Id) Tag(x, y, Text.Of("requests.pinned"), Gold, 38);
        y += 38 + 22;
        UiShapes.Fill(new Rectangle(x, y, room, 3), 1.5f, Rule);
        y += 24;

        Label(Text.Of("requests.steps"), x, y);
        y += 36;
        var next = Requests.Next(request, screen.Story);
        foreach (var step in request.Steps)
        {
            bool stepDone = Requests.IsDone(request, step, screen.Story);
            bool isNext = step == next;
            var mark = new Vector2(x + 14, y + 18);
            if (stepDone)
            {
                UiShapes.Circle(mark, 14, Green);
                UiShapes.Line(mark + new Vector2(-6, 0), mark + new Vector2(-1.5f, 5), 3.5f, Color.White);
                UiShapes.Line(mark + new Vector2(-1.5f, 5), mark + new Vector2(7, -5), 3.5f, Color.White);
            }
            else if (isNext)
            {
                UiShapes.Ring(mark, 14, 4, Selection);
                UiShapes.Circle(mark, 6, Selection);
            }
            else UiShapes.Ring(mark, 14, 3, Rule);
            string line = request.StepLine(step);
            var weight = isNext ? UiWeight.ExtraBold : UiWeight.Bold;
            var lines = Wrap(line, room - 48, 28, weight);
            DrawWrapped(line, x + 48, y + 2, room - 48, 28, isNext ? Ink : Muted, 36, weight);
            y += Math.Max(1, lines.Count) * 36 + 14;
        }

        y += 10;
        Label(Text.Of("requests.reward"), x, y);
        DrawWrapped(request.Reward, x, y + 32, room, 28, Ink, 38, UiWeight.ExtraBold);
    }

    /// <summary>THIS AREA (plan 12 · Q22): the area's name and its three counts as cards, never a name of what is left.</summary>
    private static void AreaTab(Rectangle panel, RequestsScreen screen)
    {
        Panel(panel, 34);
        float x = panel.X + 56, room = panel.Width - 112;
        UiFonts.DrawCentered(screen.AreaName, x, panel.Y + 74, 48, Ink, UiWeight.Black);
        UiShapes.Fill(new Rectangle(x, panel.Y + 128, room, 3), 1.5f, Rule);
        var counts = screen.Counts ?? default;
        var cards = new (string Label, int Count, Color Colour)[]
        {
            (Text.Of("requests.area.items"), counts.Items, Gold),
            (Text.Of("requests.area.trainers"), counts.Trainers, Red),
            (Text.Of("requests.area.pokemon"), counts.Species, Green)
        };
        float cardW = (room - 2 * Gutter) / 3f, cardY = panel.Y + 170, cardH = 300;
        for (int i = 0; i < cards.Length; i++)
        {
            var card = new Rectangle(x + i * (cardW + Gutter), cardY, cardW, cardH);
            var (label, count, colour) = cards[i];
            UiShapes.Fill(card, 30, Lighter(colour, 0.84f));
            UiShapes.Fill(new Rectangle(card.X, card.Y, card.Width, 14), 7, colour);
            float lw = UiFonts.Measure(label, 28, UiWeight.Black);
            UiFonts.DrawCentered(label, card.X + (card.Width - lw) / 2f, card.Y + 66, 28, Darker(colour, 0.3f), UiWeight.Black);
            string n = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            float nw = UiFonts.Measure(n, 120, UiWeight.Black);
            UiFonts.DrawCentered(n, card.X + (card.Width - nw) / 2f, card.Y + 182, 120, count == 0 ? Muted : Ink, UiWeight.Black);
        }
        string note = counts.Nothing ? Text.Of("leftHere.nothing") : Text.Of("requests.area.note");
        DrawWrapped(note, x, cardY + cardH + 48, room, 30, Muted, 42);
    }

    /// <summary>The height of the pinned request's panel: a caption and one line, as the start menu's small panels.</summary>
    public const float PinnedHeight = LeftHereHeight;

    /// <summary>
    /// A request pinned (plan 12 · Q21; style guide, "A request pinned"): at the top left of the field, where the location
    /// sign drops in (and never while it shows), a panel with the pin on a Gold chip, the request's title as the kit's
    /// caption and its next step under it in 24 ExtraBold Ink.
    /// </summary>
    public static void PinnedRequest(string title, string step)
    {
        float textW = Math.Max(UiFonts.Measure(title, 20, UiWeight.Black), UiFonts.Measure(step, NextSize, UiWeight.ExtraBold));
        var r = new Rectangle(48, 40, Math.Min(900, 92 + textW + 32), PinnedHeight);
        Panel(r, 30);
        var chip = new Vector2(r.X + 46, r.Y + r.Height / 2f);
        UiShapes.Circle(chip, 22, Gold);
        UiIcons.Draw(UiIcon.Pin, chip, 11, Color.White, Gold);
        Label(title, r.X + 92, r.Y + 18);
        UiFonts.Draw(step, r.X + 92, r.Y + 48, NextSize, Ink, UiWeight.ExtraBold);
    }
}
