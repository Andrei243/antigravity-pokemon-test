using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The battle room (plan 13 · V13, V26; style guide, "Menu screens", "The battle room"): its menu, the team builder,
// the puzzles, the panels between and after a run's battles, and a puzzle's goal over its battle.
internal static partial class ModernUi
{
    /// <summary>How wide the battle room's and the builder's lists are; the detail panel takes the rest.</summary>
    public const float RoomListWidth = 1040;

    private static string Fit(string text, float room, float size)
    {
        if (UiFonts.Measure(text, size, UiWeight.Black) <= room) return text;
        while (text.Length > 1 && UiFonts.Measure(text + "…", size, UiWeight.Black) > room) text = text[..^1];
        return text.TrimEnd() + "…";
    }

    /// <summary>The icon of a built Pokémon (or a puzzle's): its form's, else its species'.</summary>
    private static Texture2D BuiltIcon(string species, string? form) => PixelArtGenerator.GetPokemonIcon(form ?? species);

    /// <summary>A row of up to six icons, each at its place, with its level under it.</summary>
    private static void IconRow(IEnumerable<(string Species, string? Form, int Level)> who, float x, float y, float width)
    {
        var list = who.Take(Party.MaxSize).ToList();
        float step = Math.Min(132, width / Math.Max(1, list.Count));
        for (int i = 0; i < list.Count; i++)
        {
            var c = new Vector2(x + step * i + 56, y + 56);
            UiShapes.Circle(c, 54, Disc);
            var icon = BuiltIcon(list[i].Species, list[i].Form);
            float w = icon.Width * 2, h = icon.Height * 2;
            Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height), new Rectangle(MathF.Round(c.X - w / 2f), MathF.Round(c.Y - h / 2f), w, h), Vector2.Zero, 0f, Color.White);
            string level = Text.Of("battleRoom.level", ("level", list[i].Level));
            UiFonts.Draw(level, c.X - UiFonts.Measure(level, 24, UiWeight.Black) / 2f, y + 120, 24, Ink, UiWeight.Black);
        }
    }

    private static Rectangle RoomList(float rise) => new(Margin, ContentTop + rise, RoomListWidth, ContentBottom - ContentTop);
    private static Rectangle RoomDetail(int sw, float rise) => new(Margin + RoomListWidth + Gutter, ContentTop + rise, sw - Margin * 2 - RoomListWidth - Gutter, ContentBottom - ContentTop);

    // ------------------------------------------------------------------ the room

    public static void DrawBattleRoom(int sw, int sh, BattleRoomScreen room, float appear = 1f)
    {
        Backdrop(sw, sh);
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        switch (room.Page)
        {
            case RoomPage.Menu: RoomMenu(sw, room, rise); break;
            case RoomPage.PickTeam: RoomPickTeam(sw, room, rise); break;
            case RoomPage.Puzzles: RoomPuzzles(sw, room, rise); break;
            default: RoomPanel(sw, sh, room, rise); break;
        }
    }

    private static void RoomMenu(int sw, BattleRoomScreen room, float rise)
    {
        ScreenTitle(Text.Of("battleRoom.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("shared.keys.z", "battleRoom.hint.pick"), Hint("shared.keys.esc", "shared.hints.back"));
        var list = RoomList(rise);
        Panel(list, 40);
        for (int i = 0; i < BattleRoomScreen.Entries.Length; i++)
        {
            var entry = BattleRoomScreen.Entries[i];
            var r = RowRect(list, i);
            string key = Key(entry);
            var ink = ListRow(r, i == room.Cursor, Text.Of($"battleRoom.menu.{key}"), nameX: 44);
            string? value = entry switch
            {
                RoomEntry.Teams => Text.Of("battleRoom.menu.teamsCount", ("n", room.Teams.Count)),
                RoomEntry.Gauntlet => Text.Of("battleRoom.menu.gauntletBest", ("n", room.Records.GauntletBest), ("of", GauntletData.Default.Order.Count)),
                RoomEntry.Streak => Text.Of("battleRoom.menu.streakBest", ("n", room.Records.StreakBest)),
                RoomEntry.Friends => "",
                RoomEntry.Daily => room.DailyRecords.On(room.Today) is { } day
                    ? Text.Of("battleRoom.daily.today", ("n", day.Wins), ("of", DailyRun.Battles))
                    : Text.Of("battleRoom.daily.notYet"),
                _ => Text.Of("battleRoom.menu.puzzlesSolved", ("n", room.Records.Puzzles.Count(p => room.PuzzleList.Any(q => q.Id == p))), ("of", room.PuzzleList.Count))
            };
            RowValue(r, value, ink, 28);
        }

        var detail = RoomDetail(sw, rise);
        Panel(detail, 40);
        var chosen = BattleRoomScreen.Entries[room.Cursor];
        float x = detail.X + 44, wide = detail.Width - 88, y = detail.Y + 48;
        string chosenKey = Key(chosen);
        UiFonts.Draw(Text.Of($"battleRoom.menu.{chosenKey}"), x, y, 44, Ink, UiWeight.Black);
        UiShapes.Fill(new Rectangle(x, y + 72, wide, 3), 1.5f, Rule);
        DrawWrapped(Text.Of($"battleRoom.blurb.{chosenKey}"), x, y + 100, wide, 30, Ink, 42);
        if (chosen == RoomEntry.Gauntlet && room.Records.GauntletClears > 0)
            Tag(x, detail.Y + detail.Height - 96, Text.Of("battleRoom.clears", ("n", room.Records.GauntletClears)), Gold, 44);
        if (chosen == RoomEntry.Daily) DailyDetail(room, x, wide, detail);
    }

    // The daily run's detail (plan 13 · V14): the day and its round in tags, today's result or that the day is still to play, the best and the clears
    private static void DailyDetail(BattleRoomScreen room, float x, float wide, Rectangle detail)
    {
        float y = detail.Y + detail.Height - 300;
        UiShapes.Fill(new Rectangle(x, y, wide, 3), 1.5f, Rule);
        float w = Tag(x, y + 28, Words.Date(room.Today), Blue, 40);
        Tag(x + w + 16, y + 28, Text.Of("battleRoom.daily.round", ("n", DailyRun.RoundFor(room.Today) + 1)), Frame, 40);
        var day = room.DailyRecords.On(room.Today);
        string line = day == null ? Text.Of("battleRoom.daily.fresh") : Text.Of("battleRoom.daily.played", ("n", day.Wins), ("of", DailyRun.Battles));
        UiFonts.Draw(line, x, y + 96, 32, day == null ? Green : Ink, UiWeight.ExtraBold);
        string records = Text.Of("battleRoom.daily.records", ("best", room.DailyRecords.Best), ("clears", room.DailyRecords.Clears), ("days", room.DailyRecords.Days.Count));
        UiFonts.Draw(records, x, y + 148, 28, Muted, UiWeight.Bold);
    }

    private static string Key(RoomEntry entry) => entry switch
    {
        RoomEntry.Teams => "teams",
        RoomEntry.Gauntlet => "gauntlet",
        RoomEntry.Streak => "streak",
        RoomEntry.Friends => "friends",
        RoomEntry.Daily => "daily",
        _ => "puzzles"
    };

    private static void RoomPickTeam(int sw, BattleRoomScreen room, float rise)
    {
        ScreenTitle(Text.Of(room.Picking == RoomChallenge.Gauntlet ? "battleRoom.pick.gauntlet" : room.Picking == RoomChallenge.Friend ? "battleRoom.pick.friend" : "battleRoom.pick.streak"));
        Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("shared.keys.z", "battleRoom.hint.begin"), Hint("shared.keys.esc", "shared.hints.back"));
        var list = RoomList(rise);
        Panel(list, 40);
        if (room.Teams.Count == 0)
        {
            EmptyNote(list, Text.Of("battleRoom.noTeams"));
            return;
        }
        int visible = BattleRoomScreen.VisibleRows;
        for (int row = 0; row < visible && room.FirstRow + row < room.Teams.Count; row++)
        {
            int index = room.FirstRow + row;
            var team = room.Teams[index];
            var r = RowRect(list, row, scrolls: room.Teams.Count > visible);
            bool ok = BuiltTeams.IsLegal(team);
            var ink = ListRow(r, index == room.Cursor, Fit(team.Name, r.Width - 104 - 220, 34), enabled: ok);
            if (team.Members.Count > 0) RowIcon(r, BuiltIcon(team.Members[0].Species, team.Members[0].Form), 1, index == room.Cursor, faded: !ok, hop: Hop(index == room.Cursor));
            RowValue(r, Text.Of("battleRoom.members", ("n", team.Members.Count)), ok ? ink : index == room.Cursor ? ink : Red, 26);
        }
        ScrollBar(list, room.FirstRow, visible, room.Teams.Count);
        TeamDetail(RoomDetail(sw, rise), room.Cursor < room.Teams.Count ? room.Teams[room.Cursor] : null);
    }

    /// <summary>A team on the right: its name, its six with their levels, and why it can't battle where it can't.</summary>
    private static void TeamDetail(Rectangle detail, BuiltTeam? team)
    {
        Panel(detail, 40);
        if (team == null) return;
        float x = detail.X + 44, wide = detail.Width - 88, y = detail.Y + 48;
        UiFonts.Draw(Fit(team.Name, wide, 44), x, y, 44, Ink, UiWeight.Black);
        UiShapes.Fill(new Rectangle(x, y + 72, wide, 3), 1.5f, Rule);
        IconRow(team.Members.Select(m => (m.Species, m.Form, m.Level)), x - 8, y + 100, wide);
        if (BuiltTeams.WhyNot(team) is { } why)
        {
            Tag(x, y + 300, Text.Of("battleRoom.cannot"), Red, 44);
            DrawWrapped(Text.Of(why.Key, ("value", why.Value)), x, y + 370, wide, 30, Ink, 42);
        }
        else Tag(x, y + 300, Text.Of("battleRoom.ready"), Green, 44);
    }

    private static void RoomPuzzles(int sw, BattleRoomScreen room, float rise)
    {
        ScreenTitle(Text.Of("battleRoom.menu.puzzles"));
        Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("shared.keys.z", "battleRoom.hint.play"), Hint("shared.keys.esc", "shared.hints.back"));
        var list = RoomList(rise);
        Panel(list, 40);
        int visible = BattleRoomScreen.VisibleRows;
        for (int row = 0; row < visible && room.FirstRow + row < room.PuzzleList.Count; row++)
        {
            int index = room.FirstRow + row;
            var puzzle = room.PuzzleList[index];
            var r = RowRect(list, row, scrolls: room.PuzzleList.Count > visible);
            bool selected = index == room.Cursor;
            var ink = ListRow(r, selected, Fit(Text.Of(puzzle.TitleKey), r.Width - 104 - 120, 34));
            UiFonts.DrawCentered((index + 1).ToString("D2"), r.X + 34, r.Y + r.Height / 2f, 28, selected ? ink : Muted, UiWeight.Black);
            if (room.Records.Solved(puzzle.Id)) Star(new Vector2(r.X + r.Width - 60, r.Y + r.Height / 2f), 22, selected ? Color.White : Gold);
        }
        ScrollBar(list, room.FirstRow, visible, room.PuzzleList.Count);

        var detail = RoomDetail(sw, rise);
        Panel(detail, 40);
        if (room.Cursor >= room.PuzzleList.Count) return;
        var shown = room.PuzzleList[room.Cursor];
        float x = detail.X + 44, wide = detail.Width - 88, y = detail.Y + 48;
        UiFonts.Draw(Fit(Text.Of(shown.TitleKey), wide, 44), x, y, 44, Ink, UiWeight.Black);
        float tagW = Tag(x, y + 70, PuzzleGoalText(shown), Blue, 40);
        Tag(x + tagW + 16, y + 70, Text.Of(shown.RulesSet == Ruleset.Platinum ? "battleRoom.rules.platinum" : "battleRoom.rules.modern"), Frame, 40);
        UiShapes.Fill(new Rectangle(x, y + 136, wide, 3), 1.5f, Rule);
        DrawWrapped(Text.Of(shown.HintKey), x, y + 162, wide, 30, Ink, 42);
        float teamY = detail.Y + detail.Height - 400;
        Label(Text.Of("battleRoom.puzzle.theirs"), x, teamY);
        IconRow(shown.Foe.Select(p => (p.Species, p.Form, p.Level)), x - 8, teamY + 28, wide);
        Label(Text.Of("battleRoom.puzzle.yours"), x, teamY + 190);
        IconRow(shown.Player.Select(p => (p.Species, p.Form, p.Level)), x - 8, teamY + 218, wide);
        if (room.Records.Solved(shown.Id)) Tag(x + wide - 200, y, Text.Of("battleRoom.solved"), Gold, 44);
    }

    /// <summary>A puzzle's goal in a line: "Win in 1 turn", "Survive 2 turns", "Catch it within 2 turns".</summary>
    public static string PuzzleGoalText(PuzzleFile puzzle) => puzzle.Goal switch
    {
        PuzzleGoal.Survive => Text.Of(puzzle.Turns == 1 ? "battleRoom.goal.surviveOne" : "battleRoom.goal.survive", ("n", puzzle.Turns)),
        PuzzleGoal.Catch => Text.Of(puzzle.Turns == 1 ? "battleRoom.goal.catchOne" : "battleRoom.goal.catch", ("n", puzzle.Turns)),
        _ => Text.Of(puzzle.Turns == 1 ? "battleRoom.goal.winOne" : "battleRoom.goal.win", ("n", puzzle.Turns))
    };

    // Between two battles, a run's end and a puzzle's: one panel in the middle with its rows under it
    private static void RoomPanel(int sw, int sh, BattleRoomScreen room, float rise)
    {
        var r = new Rectangle(sw / 2f - 560, 200 + rise, 1120, 640);
        Panel(r, 40);
        float x = r.X + 56, wide = r.Width - 112, y = r.Y + 52;
        string[] rows;
        if (room.Page == RoomPage.PuzzleOver && room.LastPuzzle is { } puzzle)
        {
            ScreenTitle(Text.Of(puzzle.TitleKey));
            UiFonts.Draw(Text.Of(room.PuzzleSolved ? "battleRoom.puzzle.solved" : "battleRoom.puzzle.failed"), x, y, 56, room.PuzzleSolved ? Green : Red, UiWeight.Black);
            Tag(x, y + 92, PuzzleGoalText(puzzle), Blue, 40);
            UiShapes.Fill(new Rectangle(x, y + 156, wide, 3), 1.5f, Rule);
            DrawWrapped(Text.Of(room.PuzzleSolved ? "battleRoom.puzzle.solvedBlurb" : puzzle.HintKey), x, y + 184, wide, 30, Ink, 42);
            rows = new[] { Text.Of("battleRoom.puzzle.again"), Text.Of("battleRoom.puzzle.back") };
        }
        else if (room.Daily is { } daily)
        {
            ScreenTitle(Text.Of("battleRoom.menu.daily"));
            string heading = daily.Cleared ? Text.Of("battleRoom.daily.cleared")
                : daily.Over ? Text.Of("battleRoom.run.over", ("n", daily.Wins))
                : Text.Of("battleRoom.run.won", ("n", daily.Wins));
            UiFonts.Draw(heading, x, y, 52, daily.Cleared ? Gold : daily.Over ? Red : Ink, UiWeight.Black);
            float w = Tag(x, y + 88, Text.Of("battleRoom.run.progress", ("n", daily.Wins), ("of", DailyRun.Battles)), Blue, 40);
            if (!daily.Counts) w += 16 + Tag(x + w + 16, y + 88, Text.Of("battleRoom.daily.practice"), Frame, 40);
            if (room.NewBest && daily.Over) Tag(x + w + 16, y + 88, Text.Of("battleRoom.run.newBest"), Gold, 40);
            UiShapes.Fill(new Rectangle(x, y + 152, wide, 3), 1.5f, Rule);
            Label(Text.Of("battleRoom.run.team"), x, y + 176);
            IconRow(daily.Factory.Team.Select(r => (r.Species, (string?)null, daily.Factory.Level)), x - 8, y + 204, wide);
            if (!daily.Over)
            {
                var next = daily.NextTrainer;
                UiFonts.Draw(Text.Of("battleRoom.run.next", ("who", Trainer.ClassName(next.Class) + " " + next.Name)), x, y + 360, 32, Ink, UiWeight.ExtraBold);
                rows = new[] { Text.Of("battleRoom.run.continue"), Text.Of("battleRoom.daily.trade"), Text.Of("battleRoom.run.giveUp") };
            }
            else rows = new[] { Text.Of("battleRoom.run.ok") };
            for (int i = 0; i < rows.Length; i++)
            {
                var row = new Rectangle(r.X + 40, r.Y + r.Height - 40 - (rows.Length - i) * RowPitch, r.Width - 80, RowHeight);
                ListRow(row, i == room.Cursor, rows[i], nameX: 44, enabled: !(rows.Length == 3 && i == 1 && !daily.CanTrade));
            }
            return;
        }
        else if (room.Run is { } run)
        {
            ScreenTitle(Text.Of(run.Kind == RoomChallenge.Gauntlet ? "battleRoom.menu.gauntlet" : "battleRoom.menu.streak"));
            string heading = run.Cleared ? Text.Of("battleRoom.run.cleared")
                : run.Over ? Text.Of("battleRoom.run.over", ("n", run.Wins))
                : Text.Of("battleRoom.run.won", ("n", run.Wins));
            UiFonts.Draw(heading, x, y, 52, run.Cleared ? Gold : run.Over ? Red : Ink, UiWeight.Black);
            string progress = run.Length is { } length ? Text.Of("battleRoom.run.progress", ("n", run.Wins), ("of", length)) : Text.Of("battleRoom.run.streak", ("n", run.Wins));
            float w = Tag(x, y + 88, progress, Blue, 40);
            if (room.NewBest && run.Over) Tag(x + w + 16, y + 88, Text.Of("battleRoom.run.newBest"), Gold, 40);
            UiShapes.Fill(new Rectangle(x, y + 152, wide, 3), 1.5f, Rule);
            Label(Text.Of("battleRoom.run.team"), x, y + 176);
            IconRow(run.Team.Members.Select(m => (m.Species, m.Form, run.Kind == RoomChallenge.Streak ? RoomRun.StreakLevel : m.Level)), x - 8, y + 204, wide);
            if (!run.Over)
            {
                var foe = run.NextFoe();
                UiFonts.Draw(Text.Of("battleRoom.run.next", ("who", Trainer.ClassName(foe.TrainerClass) + " " + foe.Name)), x, y + 360, 32, Ink, UiWeight.ExtraBold);
                rows = new[] { Text.Of("battleRoom.run.continue"), Text.Of("battleRoom.run.giveUp") };
            }
            else rows = new[] { Text.Of("battleRoom.run.ok") };
        }
        else return;

        for (int i = 0; i < rows.Length; i++)
        {
            var row = new Rectangle(r.X + 40, r.Y + r.Height - 40 - (rows.Length - i) * RowPitch, r.Width - 80, RowHeight);
            ListRow(row, i == room.Cursor, rows[i], nameX: 44);
        }
    }

    // ------------------------------------------------------------------ the team builder

    public static void DrawTeamBuilder(int sw, int sh, TeamBuilderScreen b, float appear = 1f)
    {
        if (b.Page == BuilderPage.Name && b.Entry is { } entry)
        {
            Backdrop(sw, sh);
            DrawNameEntry(sw, sh, entry, null, PlayerIdentity.Look, 1f, Text.Of("battleRoom.builder.nameTitle"), b.Team?.Name ?? "");
            return;
        }
        Backdrop(sw, sh);
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var list = RoomList(rise);
        var detail = RoomDetail(sw, rise);
        Panel(list, 40);
        int visible = TeamBuilderScreen.VisibleRows;
        switch (b.Page)
        {
            case BuilderPage.Teams:
                ScreenTitle(Text.Of("battleRoom.builder.title"));
                Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("shared.keys.z", "battleRoom.hint.pick"), Hint("shared.keys.esc", "shared.hints.back"));
                for (int row = 0; row < visible && b.FirstRow + row < b.TeamsRows; row++)
                {
                    int i = b.FirstRow + row;
                    var r = RowRect(list, row, scrolls: b.TeamsRows > visible);
                    if (i < b.File.Teams.Count)
                    {
                        var team = b.File.Teams[i];
                        var ink = ListRow(r, i == b.Cursor, Fit(team.Name, r.Width - 330, 34));
                        if (team.Members.Count > 0) RowIcon(r, BuiltIcon(team.Members[0].Species, team.Members[0].Form), 1, i == b.Cursor, hop: Hop(i == b.Cursor));
                        RowValue(r, Text.Of("battleRoom.members", ("n", team.Members.Count)), BuiltTeams.IsLegal(team) || i == b.Cursor ? ink : Red, 26);
                    }
                    else ListRow(r, i == b.Cursor, Text.Of("battleRoom.builder.newTeam"), nameX: 44);
                }
                ScrollBar(list, b.FirstRow, visible, b.TeamsRows);
                TeamDetail(detail, b.Cursor < b.File.Teams.Count ? b.File.Teams[b.Cursor] : null);
                break;
            case BuilderPage.Team when b.Team is { } t:
                ScreenTitle(Fit(t.Name, 900, 52));
                Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("shared.keys.z", "battleRoom.hint.pick"), Hint("shared.keys.esc", "shared.hints.back"));
                for (int row = 0; row < visible && b.FirstRow + row < b.TeamRows; row++)
                {
                    int i = b.FirstRow + row;
                    var r = RowRect(list, row);
                    int what = b.TeamRowAt(i);
                    bool selected = i == b.Cursor;
                    if (what >= 0)
                    {
                        var m = t.Members[what];
                        var ink = ListRow(r, selected, Fit(MemberName(m), r.Width - 300, 34));
                        RowIcon(r, BuiltIcon(m.Species, m.Form), 1, selected, faded: BuiltTeams.WhyNot(m) != null, hop: Hop(selected));
                        RowValue(r, Text.Of("battleRoom.level", ("level", m.Level)), BuiltTeams.WhyNot(m) == null || selected ? ink : Red, 28);
                    }
                    else if (what == -1) ListRow(r, selected, Text.Of("battleRoom.builder.add"), nameX: 44);
                    else if (what == -2) ListRow(r, selected, Text.Of("battleRoom.builder.rename"), nameX: 44);
                    else
                    {
                        var ink = ListRow(r, selected, Text.Of(b.DeleteArmed ? "battleRoom.builder.deleteAgain" : "battleRoom.builder.delete"), nameX: 44);
                        if (!selected) UiFonts.DrawCentered(Text.Of("battleRoom.builder.delete"), r.X + 44, r.Y + r.Height / 2f, 34, Red, UiWeight.Black);
                    }
                }
                int shownMember = b.TeamRowAt(b.Cursor);
                MemberDetail(detail, shownMember >= 0 ? t.Members[shownMember] : null);
                break;
            case BuilderPage.Member when b.Member is { } member:
                ScreenTitle(Fit(MemberName(member), 900, 52));
                Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("shared.keys.leftRight", "battleRoom.hint.change"), Hint("shared.keys.z", "battleRoom.hint.pick"), Hint("shared.keys.esc", "shared.hints.back"));
                int memberRows = TeamBuilderScreen.MemberRows.Length;
                for (int row = 0; row < visible && b.FirstRow + row < memberRows; row++)
                {
                    int index = b.FirstRow + row;
                    var which = TeamBuilderScreen.MemberRows[index];
                    var r = RowRect(list, row, scrolls: memberRows > visible);
                    bool selected = index == b.Cursor;
                    string rowKey = RowKey(which);
                    var ink = ListRow(r, selected, Text.Of($"battleRoom.row.{rowKey}"), nameX: 44, size: 30);
                    string value = MemberValue(member, which);
                    if (value.Length > 0) RowValue(r, Fit(value, r.Width - 340, 28), which == MemberRow.Remove ? (selected ? ink : Red) : ink, 28);
                }
                ScrollBar(list, b.FirstRow, visible, memberRows);
                MemberDetail(detail, member);
                break;
            case BuilderPage.Pick:
                string picking = PickKey(b.Picking);
                ScreenTitle(Text.Of($"battleRoom.pick.{picking}Title"));
                Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("jukebox.keys.shoulders", "battleRoom.hint.jump"), Hint("shared.keys.z", "battleRoom.hint.pick"), Hint("shared.keys.esc", "shared.hints.back"));
                for (int row = 0; row < visible && b.FirstRow + row < b.Options.Count; row++)
                {
                    int i = b.FirstRow + row;
                    var option = b.Options[i];
                    var r = RowRect(list, row, scrolls: b.Options.Count > visible);
                    bool selected = i == b.Cursor;
                    if (b.Picking == PickKind.Species && option.Value != null)
                    {
                        ListRow(r, selected, Fit(option.Label, r.Width - 140, 32), size: 32);
                        RowIcon(r, BuiltIcon(option.Value, null), 1, selected, hop: Hop(selected));
                    }
                    else ListRow(r, selected, Fit(option.Label, r.Width - 80, 32), nameX: 44, size: 32);
                }
                ScrollBar(list, b.FirstRow, visible, b.Options.Count);
                PickDetail(detail, b);
                break;
        }
    }

    private static string RowKey(MemberRow row) => row switch
    {
        MemberRow.Move1 or MemberRow.Move2 or MemberRow.Move3 or MemberRow.Move4 => "move",
        _ => row.ToString().ToLowerInvariant()
    };

    private static string PickKey(PickKind kind) => kind switch
    {
        PickKind.Species => "species",
        PickKind.Item => "item",
        _ => "move"
    };

    private static string MemberName(BuiltPokemon m) =>
        m.Form != null && PokemonDatabase.Get(m.Species)?.Form(m.Form) is { } form ? form.DisplayName(m.Species) : LocalNames.Species(m.Species);

    private static string MemberValue(BuiltPokemon m, MemberRow row) => row switch
    {
        MemberRow.Species => LocalNames.Species(m.Species),
        MemberRow.Form => m.Form != null && PokemonDatabase.Get(m.Species)?.Form(m.Form) is { } form ? form.DisplayName(m.Species) : Text.Of("battleRoom.builder.none"),
        MemberRow.Level => m.Level.ToString(),
        MemberRow.Nature => Words.Nature(m.Nature),
        MemberRow.Ability => m.Ability is { } a ? LocalNames.Ability(a) : BuiltTeams.AbilitiesOf(m).FirstOrDefault() is { } first ? LocalNames.Ability(first) : "",
        MemberRow.Item => m.Item is { } i ? LocalNames.Item(i) : Text.Of("battleRoom.builder.none"),
        MemberRow.Move1 or MemberRow.Move2 or MemberRow.Move3 or MemberRow.Move4 =>
            (int)(row - MemberRow.Move1) < m.Moves.Count ? LocalNames.Move(m.Moves[row - MemberRow.Move1]) : Text.Of("battleRoom.builder.noMove"),
        _ => ""
    };

    /// <summary>A member on the right: its picture, its types, its moves, and whether it can battle.</summary>
    private static void MemberDetail(Rectangle detail, BuiltPokemon? m)
    {
        Panel(detail, 40);
        if (m == null || PokemonDatabase.Get(m.Species) == null) return;
        float x = detail.X + 44, wide = detail.Width - 88, y = detail.Y + 48;
        var pokemon = BuiltTeams.Make(m);
        var icon = BuiltIcon(m.Species, m.Form);
        var c = new Vector2(x + 90, y + 90);
        UiShapes.Circle(c, 88, Disc);
        float w = icon.Width * 3, h = icon.Height * 3;
        Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height), new Rectangle(MathF.Round(c.X - w / 2f), MathF.Round(c.Y - h / 2f), w, h), Vector2.Zero, 0f, Color.White);
        UiFonts.Draw(Fit(MemberName(m), wide - 220, 40), x + 210, y + 20, 40, Ink, UiWeight.Black);
        TypePills(x + 210, y + 90, pokemon, 36);
        UiShapes.Fill(new Rectangle(x, y + 200, wide, 3), 1.5f, Rule);
        for (int i = 0; i < pokemon.Moves.Count; i++)
        {
            var move = pokemon.Moves[i];
            float my = y + 226 + i * 60;
            float tw = TypePill(x, my, move.Data.Type, 40, 150);
            UiFonts.DrawCentered(Fit(move.DisplayName, wide - tw - 40, 30), x + tw + 20, my + 20, 30, Ink, UiWeight.Black);
        }
        string stats = Text.Of("battleRoom.builder.stats", ("hp", pokemon.MaxHP), ("atk", pokemon.Attack), ("def", pokemon.Defense), ("spa", pokemon.SpAttack), ("spd", pokemon.SpDefense), ("spe", pokemon.Speed));
        UiFonts.Draw(Fit(stats, wide, 26), x, y + 480, 26, Muted, UiWeight.ExtraBold);
        if (BuiltTeams.WhyNot(m) is { } why)
        {
            Tag(x, detail.Y + detail.Height - 210, Text.Of("battleRoom.cannot"), Red, 44);
            DrawWrapped(Text.Of(why.Key, ("value", why.Value)), x, detail.Y + detail.Height - 150, wide, 28, Ink, 38);
        }
        else Tag(x, detail.Y + detail.Height - 96, Text.Of("battleRoom.ready"), Green, 44);
    }

    // What the option under the cursor is: the species' picture and types, a move's type, power and accuracy
    private static void PickDetail(Rectangle detail, TeamBuilderScreen b)
    {
        Panel(detail, 40);
        if (b.Cursor >= b.Options.Count) return;
        var option = b.Options[b.Cursor];
        float x = detail.X + 44, wide = detail.Width - 88, y = detail.Y + 48;
        UiFonts.Draw(Fit(option.Label, wide, 40), x, y, 40, Ink, UiWeight.Black);
        if (option.Value == null) return;
        switch (b.Picking)
        {
            case PickKind.Species when PokemonDatabase.Get(option.Value) is { } species:
            {
                var icon = BuiltIcon(species.Name, null);
                var c = new Vector2(x + 120, y + 200);
                UiShapes.Circle(c, 110, Disc);
                float w = icon.Width * 4, h = icon.Height * 4;
                Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height), new Rectangle(MathF.Round(c.X - w / 2f), MathF.Round(c.Y - h / 2f), w, h), Vector2.Zero, 0f, Color.White);
                float tx = x + 270;
                tx += TypePill(tx, y + 100, species.PrimaryType, 40) + 12;
                if (species.SecondaryType is { } second) TypePill(tx, y + 100, second, 40);
                break;
            }
            case PickKind.Move when MoveDatabase.Has(option.Value):
            {
                var move = MoveDatabase.Get(option.Value);
                TypePill(x, y + 70, move.Type, 40, 160);
                string line = Text.Of("battleRoom.builder.moveLine", ("power", move.Power > 0 ? move.Power.ToString() : "—"), ("accuracy", move.Accuracy > 0 ? move.Accuracy.ToString() : "—"), ("pp", move.MaxPP));
                UiFonts.Draw(line, x, y + 140, 30, Ink, UiWeight.ExtraBold);
                break;
            }
        }
    }

    // ------------------------------------------------------------------ over a puzzle's battle

    /// <summary>A puzzle's goal over its battle: a Blue pill at the top middle, where neither health box stands.</summary>
    public static void PuzzleGoalLine(int sw, PuzzleFile puzzle, int turnsDone)
    {
        string goal = PuzzleGoalText(puzzle);
        string turn = Text.Of("battleRoom.goal.turn", ("n", Math.Min(turnsDone + 1, puzzle.Turns)), ("of", puzzle.Turns));
        float size = 30;
        float w = UiFonts.Measure(goal, size, UiWeight.Black) + UiFonts.Measure(turn, 24, UiWeight.ExtraBold) + 96;
        var r = new Rectangle(sw / 2f - w / 2f, 18, w, 60);
        UiShapes.Shadow(r, 30, 14, new Vector2(0, 4), ShadowColor);
        UiShapes.Shape(r, 30, Lighter(Blue, 0.1f), Darker(Blue, 0.08f), Darker(Blue, 0.35f), 3);
        UiFonts.DrawCentered(goal, r.X + 32, r.Y + r.Height / 2f, size, Color.White, UiWeight.Black);
        UiFonts.DrawCentered(turn, r.X + r.Width - 32 - UiFonts.Measure(turn, 24, UiWeight.ExtraBold), r.Y + r.Height / 2f, 24, new Color(220, 232, 255, 255), UiWeight.ExtraBold);
    }
}
