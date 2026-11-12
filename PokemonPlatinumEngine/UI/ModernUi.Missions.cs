using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The hub's Week's Work and Pennant Hall's pennants on screen (plan 41 · EG6, EG3; style guide, "Menu screens (G10)",
/// "The Week's Work", and "Field menus and notices", "Pennants and a timed mission's corner").
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The pennant's red, and the board's cork.</summary>
    private static readonly Color PennantRed = new(214, 82, 66, 255);
    private static readonly Color Cork = new(196, 150, 98, 255);

    /// <summary>A pennant: a short pole and a red triangle flying from it, <paramref name="size"/> tall, its pole at <paramref name="x"/>.</summary>
    public static void Pennant(float x, float y, float size, Color? colour = null)
    {
        var red = colour ?? PennantRed;
        UiShapes.Fill(new Rectangle(x - size * 0.05f, y, size * 0.1f, size), size * 0.05f, Frame);
        UiShapes.Triangle(new Vector2(x + size * 0.05f, y + size * 0.04f), new Vector2(x + size * 0.85f, y + size * 0.26f),
            new Vector2(x + size * 0.05f, y + size * 0.5f), red, size * 0.04f);
    }

    /// <summary>A number of pennants: the pennant and the count after it. Returns its width.</summary>
    public static float Pennants(float x, float centerY, int amount, float size, Color ink)
    {
        Pennant(x, centerY - size * 0.5f, size);
        string text = amount.ToString("N0");
        UiFonts.DrawCentered(text, x + size * 0.95f, centerY, size, ink, UiWeight.Black);
        return size * 0.95f + UiFonts.Measure(text, size, UiWeight.Black);
    }

    /// <summary>The pennants held, standing over the field while Pennant Hall's people speak of them (where the Battle Points stand in a battle room).</summary>
    public static void PennantsCount(int pennants)
    {
        var r = new Rectangle(32, 32, 300, 96);
        Panel(r, 24);
        Label(Text.Of("hubPlaza.pennants"), r.X + 28, r.Y + 18);
        Pennants(r.X + 34, r.Y + 64, pennants, 36, Ink);
    }

    /// <summary>A timed mission under way: what it asks done so far and the time left, at the field's top right (the Bug-Catching Contest's corner).</summary>
    public static void MissionCorner(int sw, Mission mission, float secondsLeft)
    {
        var r = new Rectangle(sw - 32 - 340, 32, 340, 96);
        Panel(r, 24);
        Label(Text.Of("hubMissions.corner.done"), r.X + 28, r.Y + 18);
        UiFonts.Draw($"{Math.Min(mission.Progress, mission.Goal)}/{mission.Goal}", r.X + 28, r.Y + 44, 36, Ink, UiWeight.Black);
        Label(Text.Of("hubMissions.corner.left"), r.X + 196, r.Y + 18);
        int seconds = (int)MathF.Ceiling(secondsLeft);
        UiFonts.Draw($"{seconds / 60}:{seconds % 60:D2}", r.X + 196, r.Y + 44, 36, secondsLeft <= mission.Minutes * 12f ? Red : Ink, UiWeight.Black);
    }

    /// <summary>
    /// The Week's Work: a row to each mission, what it asks with its region's tag, a meter of what is done and its reward;
    /// beside the list, the one under the cursor large with its place and its clock, the pennants held and the hall's
    /// rank; the last press's words under it all.
    /// </summary>
    public static void DrawMissionBoard(int sw, int sh, MissionBoardScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("hubMissions.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.move"), Hint("shared.keys.z", "hubMissions.hint.act"), Hint("shared.keys.esc", "shared.hints.back"));
        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        const float listW = 1080;
        var list = new Rectangle(Margin - slide, ContentTop, listW, 5 * RowPitch + 140);
        var detail = new Rectangle(Margin + listW + Gutter + slide, ContentTop, sw - Margin * 2 - listW - Gutter, ContentBottom - ContentTop - 120);
        Panel(list, 34);
        Panel(detail, 34);
        // The week's heading: from its Monday to its Sunday
        var monday = MissionWeek.MondayOf(GameClock.Today);
        Label(Text.Of("hubMissions.week", ("from", Words.ShortDate(monday)), ("to", Words.ShortDate(monday.AddDays(6)))), list.X + 44, list.Y + 30);

        var missions = screen.Board.Missions;
        if (missions.Count == 0) DrawWrapped(Text.Of("hubMissions.empty"), list.X + 44, list.Y + 90, list.Width - 88, 32, Muted, 42);
        for (int row = 0; row < missions.Count && row < 5; row++)
        {
            var m = missions[row];
            var r = RowRect(list, row, 70);
            bool selected = row == screen.Cursor;
            var ink = ListRow(r, selected, Shorten(MissionWeek.Describe(m), 30, 640), enabled: !m.Claimed, nameX: 40, size: 30);
            // Its meter, and its state or its reward at the right
            var meter = new Rectangle(r.X + 700, r.Y + r.Height / 2f - 10, 150, 20);
            Bar(meter, m.Goal == 0 ? 0f : Math.Min(1f, (float)m.Progress / m.Goal), m.IsDone ? Green : m.IsTimed ? Gold : Blue);
            string state = m.Claimed ? Text.Of("hubMissions.state.claimed")
                : m.IsDone ? Text.Of("hubMissions.state.claim")
                : m.IsTimed && screen.Board.Running == m ? Text.Of("hubMissions.state.running")
                : m.IsTimed ? Text.Of("hubMissions.state.begin") : "";
            if (state.Length > 0) RowValue(r, state, selected ? Color.White : m.IsDone && !m.Claimed ? Green : ink, 26);
            else Pennants(r.X + r.Width - 120, r.Y + r.Height / 2f, m.Reward, 28, ink);
        }

        // The one under the cursor
        if (screen.Selected is { } chosen)
        {
            float x = detail.X + 44, room = detail.Width - 88;
            Label(Text.Of(chosen.IsTimed ? "hubMissions.timed" : "hubMissions.plain"), x, detail.Y + 30);
            string region = RegionDatabase.Get(chosen.Region)?.Name ?? chosen.Region;
            Tag(detail.X + detail.Width - 44 - UiFonts.Measure(region, 24, UiWeight.Black) - 34, detail.Y + 26, region, Frame, 40);
            DrawWrapped(MissionWeek.Describe(chosen), x, detail.Y + 90, room, 34, Ink, 44, UiWeight.Black);
            Label(Text.Of("hubMissions.progress"), x, detail.Y + 250);
            UiFonts.Draw($"{Math.Min(chosen.Progress, chosen.Goal)}/{chosen.Goal}", x, detail.Y + 276, 40, Ink, UiWeight.Black);
            Label(Text.Of("hubMissions.reward"), x + 260, detail.Y + 250);
            float w = Pennants(x + 266, detail.Y + 300, chosen.Reward, 40, Ink);
            if (chosen.Item != null && ItemDatabase.Get(chosen.Item) is { } item)
                UiFonts.DrawCentered(Text.Of("hubMissions.andItem", ("item", item.DisplayName)), x + 280 + w, detail.Y + 300, 26, Muted, UiWeight.ExtraBold);
            if (chosen.IsTimed)
            {
                bool running = screen.Board.Running == chosen;
                Label(Text.Of("hubMissions.clock"), x, detail.Y + 360);
                int seconds = running ? (int)MathF.Ceiling(screen.Board.SecondsLeft) : chosen.Minutes * 60;
                UiFonts.Draw($"{seconds / 60}:{seconds % 60:D2}", x, detail.Y + 386, 40, running ? Gold : Ink, UiWeight.Black);
                DrawWrapped(Text.Of("hubMissions.clockNote"), x, detail.Y + 446, room, 24, Muted, 32, UiWeight.ExtraBold);
            }
        }
        // The pennants held and the hall's rank, at the panel's foot
        var foot = new Rectangle(detail.X + 32, detail.Y + detail.Height - 132, detail.Width - 64, 100);
        UiShapes.Fill(foot, 26, Rule with { A = 90 });
        Label(Text.Of("hubPlaza.pennants"), foot.X + 28, foot.Y + 16);
        Pennants(foot.X + 34, foot.Y + 64, screen.Plaza.Coins, 36, Ink);
        Label(Text.Of("hubPlaza.rank"), foot.X + 300, foot.Y + 16);
        UiFonts.Draw(screen.Plaza.Rank.ToString(), foot.X + 300, foot.Y + 42, 40, Ink, UiWeight.Black);

        // The last press's words, on cork under the list
        if (screen.Notice.Length > 0)
        {
            var note = new Rectangle(Margin - slide, list.Y + list.Height + 24, listW, 96);
            UiShapes.Fill(note, 26, Cork);
            DrawWrapped(screen.Notice, note.X + 36, note.Y + 30, note.Width - 72, 30, Color.White, 40, UiWeight.Black);
        }
    }

    /// <summary>A line cut down to fit a width, with an ellipsis where it was cut.</summary>
    private static string Shorten(string text, float size, float width)
    {
        if (UiFonts.Measure(text, size, UiWeight.Black) <= width) return text;
        while (text.Length > 1 && UiFonts.Measure(text + "…", size, UiWeight.Black) > width) text = text[..^1];
        return text.TrimEnd() + "…";
    }
}
