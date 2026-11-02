using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The Poké Jobs board (plan 15 · E3; style guide, "Menu screens", "Poké Jobs")
internal static partial class ModernUi
{
    /// <summary>A job's colour: an errand Blue, the orchard Green, the hot spring Red.</summary>
    public static Color JobColour(JobKind kind) => kind switch { JobKind.Orchard => Green, JobKind.HotSpring => Red, _ => Blue };

    private static UiIcon JobIcon(JobKind kind) => kind switch { JobKind.Orchard => UiIcon.Berry, JobKind.HotSpring => UiIcon.Disc, _ => UiIcon.Letter };

    /// <summary>Hours and minutes left, as the board says them.</summary>
    public static string TimeLeft(TimeSpan left)
    {
        int minutes = (int)Math.Ceiling(left.TotalMinutes);
        return Text.Of("jobs.left", ("hours", minutes / 60), ("minutes", minutes % 60));
    }

    public static void DrawJobs(int sw, int sh, JobsScreen screen, DateTime now, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("jobs.title"));
        var keys = new List<HintItem>();
        // The setup has two rows, the job and its hours: up and down move between them, the ring at the left says which
        if (screen.Phase == JobsPhase.Setup) keys.Add(Hint("shared.keys.upDown", "shared.hints.move"));
        if (screen.Phase == JobsPhase.Setup) keys.Add(Hint("shared.keys.leftRight", "shared.hints.change"));
        keys.Add(Hint("shared.keys.z", screen.Phase == JobsPhase.Board && screen.SelectedJob != null ? "jobs.hint.bringBack"
            : screen.Phase == JobsPhase.Setup ? "jobs.hint.send" : "shared.hints.choose"));
        keys.Add(Hint("shared.keys.esc", "shared.hints.back"));
        Hints(sw - Margin, 44, keys.ToArray());

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        var list = new Rectangle(Margin - slide, ContentTop, 1000, ContentBottom - ContentTop);
        var detail = new Rectangle(Margin + 1000 + Gutter + slide, ContentTop, sw - Margin * 2 - 1000 - Gutter, ContentBottom - ContentTop);
        Panel(list, 34);
        Panel(detail, 34);

        if (screen.Phase is JobsPhase.Board or JobsPhase.Result) JobsBoard(list, screen, now);
        else JobsChoosing(list, screen);

        switch (screen.Phase)
        {
            case JobsPhase.Setup:
                JobSetup(detail, screen);
                break;
            case JobsPhase.Choose:
                if (screen.Shown is { } candidate) JobPokemonHead(detail, candidate);
                break;
            default:
                if (screen.SelectedJob is { } job && screen.Shown is { } away) JobDetail(detail, job, away, now);
                else DrawWrapped(Text.Of("jobs.about"), detail.X + 44, detail.Y + 60, detail.Width - 88, 30, Muted, 42, UiWeight.Bold);
                break;
        }

        if (screen.Phase == JobsPhase.Result && screen.Result is { } result) JobResultPanel(sw, sh, result);
    }

    private static void JobsBoard(Rectangle list, JobsScreen screen, DateTime now)
    {
        for (int row = 0; row < JobBoard.Slots; row++)
        {
            var r = RowRect(list, row);
            bool selected = row == screen.Cursor && screen.Phase == JobsPhase.Board;
            if (row < screen.Board.Jobs.Count && row < screen.Away.Count)
            {
                var job = screen.Board.Jobs[row];
                var pokemon = screen.Away[row];
                var ink = ListRow(r, selected, pokemon.DisplayName, true);
                RowIcon(r, PixelArtGenerator.IconOf(pokemon), 1f, selected, hop: Hop(selected));
                if (job.IsDone(now)) RowValue(r, Text.Of("jobs.back"), selected ? Color.White : Green, 28);
                else RowValue(r, JobsScreen.KindName(job.Kind), selected ? Color.White : JobColour(job.Kind), 28);
            }
            else
            {
                ListRow(r, selected, Text.Of("jobs.post"), false);
                var c = new Vector2(r.X + 52, r.Y + r.Height / 2f);
                UiShapes.Circle(c, 34, selected ? Color.White : Disc);
                UiShapes.Fill(new Rectangle(c.X - 14, c.Y - 2.5f, 28, 5), 2.5f, Rule);
                UiShapes.Fill(new Rectangle(c.X - 2.5f, c.Y - 14, 5, 28), 2.5f, Rule);
            }
        }
        float y = list.Y + 24 + JobBoard.Slots * RowPitch + 24;
        UiShapes.Fill(new Rectangle(list.X + 36, y, list.Width - 72, 3), 1.5f, Rule);
        DrawWrapped(screen.Notice ?? Text.Of("jobs.boardNote"), list.X + 44, y + 28, list.Width - 88, 28, screen.Notice != null ? Ink : Muted, 40, UiWeight.ExtraBold);
    }

    private static void JobsChoosing(Rectangle list, JobsScreen screen)
    {
        var all = screen.Candidates;
        bool scrolls = all.Count > JobsScreen.VisibleRows;
        for (int row = 0; row < JobsScreen.VisibleRows && screen.ChooseFirst + row < all.Count; row++)
        {
            int index = screen.ChooseFirst + row;
            var pokemon = all[index];
            var r = RowRect(list, row, 24, scrolls);
            bool selected = index == screen.ChooseCursor;
            var ink = ListRow(r, selected, pokemon.DisplayName, screen.Phase == JobsPhase.Choose || selected);
            RowIcon(r, PixelArtGenerator.IconOf(pokemon), 1f, selected, hop: Hop(selected));
            RowValue(r, Text.Of("jobs.level", ("level", pokemon.Level)), ink, 28);
        }
        ScrollBar(list, screen.ChooseFirst, JobsScreen.VisibleRows, all.Count, 24);
    }

    /// <summary>A Pokémon's sprite on a disc with its name and level beside it; returns the y under the head.</summary>
    private static float JobPokemonHead(Rectangle detail, Pokemon pokemon)
    {
        var disc = new Vector2(detail.X + 44 + 150, detail.Y + 44 + 150);
        BallDisc(disc, 150);
        PixelArt(PixelArtGenerator.SpriteOf(pokemon), disc, 2);
        float x = disc.X + 150 + 40, room = detail.X + detail.Width - 44 - x;
        float size = 44, w = UiFonts.Measure(pokemon.DisplayName, size, UiWeight.Black);
        if (w > room) size = MathF.Floor(size * room / w);
        UiFonts.DrawCentered(pokemon.DisplayName, x, detail.Y + 110, size, Ink, UiWeight.Black);
        UiFonts.DrawCentered(Text.Of("jobs.level", ("level", pokemon.Level)), x, detail.Y + 166, 32, Muted, UiWeight.Black);
        return detail.Y + 44 + 300 + 28;
    }

    private static void JobDetail(Rectangle detail, Job job, Pokemon pokemon, DateTime now)
    {
        float y = JobPokemonHead(detail, pokemon);
        float x = detail.X + 44, room = detail.Width - 88;
        Tag(detail.X + 44 + 300 + 40, detail.Y + 210, JobsScreen.KindName(job.Kind), JobColour(job.Kind), 40);

        float fx = Field(Text.Of("jobs.hours"), Text.Of("jobs.hoursValue", ("n", job.Hours)), x, y, 36, 48);
        Field(Text.Of("jobs.dueBack"), job.IsDone(now) ? Text.Of("jobs.back") : TimeLeft(job.Left(now)), fx, y, 36);
        y += 100;
        int done = job.HoursDone(now);
        Bar(new Rectangle(x, y, room, 20), job.Hours == 0 ? 0f : (float)done / job.Hours, JobColour(job.Kind));
        UiFonts.Draw(Text.Of("jobs.hoursDone", ("done", done), ("of", job.Hours)), x, y + 34, 28, Muted, UiWeight.ExtraBold);
        string pays = KeyOf(job.Kind);
        DrawWrapped(Text.Of($"jobs.pays.{pays}"), x, y + 90, room, 26, Muted, 36, UiWeight.ExtraBold);
    }

    private static string KeyOf(JobKind kind) => kind switch { JobKind.Orchard => "orchard", JobKind.HotSpring => "hotSpring", _ => "errand" };

    private static void JobSetup(Rectangle detail, JobsScreen screen)
    {
        if (screen.Chosen is not { } pokemon) return;
        float y = JobPokemonHead(detail, pokemon);
        float x = detail.X + 44, room = detail.Width - 88;
        var kinds = Enum.GetValues<JobKind>();
        var tabs = new List<(string, UiIcon, Color)>();
        foreach (var kind in kinds) tabs.Add((JobsScreen.KindName(kind), JobIcon(kind), JobColour(kind)));
        var strip = new Rectangle(x + 40, y, room - 40, 64);
        Tabs(strip, tabs, Array.IndexOf(kinds, screen.Kind));
        if (screen.SetupRow == JobsSetupRow.Kind) UiShapes.Ring(new Vector2(x + 12, y + 32), 12, 4, Selection);

        float hy = y + 64 + 72;
        Label(Text.Of("jobs.hours"), x + 40, hy - 52);
        Stepper(new Vector2(x + 40 + 84 + 160, hy), Text.Of("jobs.hoursValue", ("n", screen.Hours)), 320, 76);
        if (screen.SetupRow == JobsSetupRow.Hours) UiShapes.Ring(new Vector2(x + 12, hy), 12, 4, Selection);

        string pays = KeyOf(screen.Kind);
        DrawWrapped(Text.Of($"jobs.pays.{pays}"), x, hy + 72, room, 26, Muted, 36, UiWeight.ExtraBold);
    }

    private static void JobResultPanel(int sw, int sh, JobResult result)
    {
        Dim(sw, sh, 150);
        var lines = new List<string> { Text.Of("jobs.result.hours", ("n", result.Hours)) };
        if (result.Exp > 0) lines.Add(Text.Of("jobs.result.exp", ("exp", result.Exp)));
        if (result.LevelsGained > 0) lines.Add(Text.Of("jobs.result.level", ("level", result.Pokemon.Level)));
        foreach (string move in result.MovesLearned) lines.Add(Text.Of("jobs.result.move", ("move", Data.LocalNames.Move(move))));
        if (result.Effort > 0) lines.Add(Text.Of("jobs.result.effort", ("n", result.Effort), ("stat", Words.Stat(result.EffortStat))));
        if (result.Friendship > 0) lines.Add(Text.Of("jobs.result.friendship"));
        foreach (var (item, count) in result.Berries) lines.Add(Text.Of("jobs.result.berry", ("item", Data.LocalNames.Item(item)), ("n", count)));

        float h = 300 + lines.Count * 44 + 40;
        var r = new Rectangle(sw / 2f - 550, sh / 2f - h / 2f, 1100, h);
        Panel(r, 36);
        var disc = new Vector2(r.X + 56 + 110, r.Y + 56 + 110);
        BallDisc(disc, 110);
        PixelArt(PixelArtGenerator.SpriteOf(result.Pokemon), disc, 2);
        UiFonts.DrawCentered(Text.Of("jobs.result.title", ("name", result.Pokemon.DisplayName)), disc.X + 110 + 40, disc.Y, 48, Ink, UiWeight.Black);
        float y = r.Y + 300;
        foreach (string line in lines)
        {
            UiFonts.Draw(line, r.X + 64, y, 30, Ink, UiWeight.ExtraBold);
            y += 44;
        }
    }
}
