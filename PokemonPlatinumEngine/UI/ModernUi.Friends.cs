using System;
using System.Linq;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The friends' page of the battle room (plan 13 · V22; style guide, "Menu screens", "The friends' page"): the trainer files
// with their teams or their reasons, the two readers of the clipboard, the teams to send and the code panel.
internal static partial class ModernUi
{
    public static void DrawFriends(int sw, int sh, FriendsScreen page, float appear = 1f)
    {
        Backdrop(sw, sh);
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        ScreenTitle(Text.Of("friends.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("shared.keys.z", "friends.hint.do"), Hint("shared.keys.esc", "shared.hints.back"));
        var list = RoomList(rise);
        Panel(list, 40);
        int visible = FriendsScreen.VisibleRows;
        for (int row = 0; row < visible && page.FirstRow + row < page.Rows.Count; row++)
        {
            int index = page.FirstRow + row;
            var r = RowRect(list, row, scrolls: page.Rows.Count > visible);
            bool selected = index == page.Cursor;
            var kind = page.Rows[index];
            switch (kind.Kind)
            {
                case FriendsRowKind.Trainer:
                {
                    var entry = page.Files[kind.Index];
                    bool ok = entry.File != null;
                    string name = entry.File?.Name ?? entry.FileName.Replace(TrainerFile.Ending, "");
                    var ink = ListRow(r, selected, Fit(name, r.Width - 104 - 220, 34), enabled: ok);
                    RowValue(r, Text.Of(ok ? "friends.row.ready" : "friends.row.refused"), ok ? ink : selected ? ink : Red, 26);
                    break;
                }
                case FriendsRowKind.ReadCode:
                    ListRow(r, selected, Text.Of("friends.row.readCode"), nameX: 44);
                    break;
                case FriendsRowKind.PasteTeam:
                    ListRow(r, selected, Text.Of("friends.row.pasteTeam"), nameX: 44);
                    break;
                default:
                {
                    var team = page.Teams.Teams[kind.Index];
                    var ink = ListRow(r, selected, Fit(Text.Of("friends.row.send", ("name", team.Name)), r.Width - 104 - 120, 34), nameX: 44);
                    if (team.Members.Count > 0) RowIcon(r, BuiltIcon(team.Members[0].Species, team.Members[0].Form), 1, selected, hop: Hop(selected));
                    break;
                }
            }
        }
        ScrollBar(list, page.FirstRow, visible, page.Rows.Count);

        var detail = RoomDetail(sw, rise);
        Panel(detail, 40);
        float x = detail.X + 44, wide = detail.Width - 88, y = detail.Y + 48;
        var shown = page.Rows[page.Cursor];
        switch (shown.Kind)
        {
            case FriendsRowKind.Trainer:
            {
                var entry = page.Files[shown.Index];
                if (entry.File is { } file)
                {
                    UiFonts.Draw(Fit(file.Name, wide, 44), x, y, 44, Ink, UiWeight.Black);
                    UiFonts.Draw(Fit(Trainer.ClassName(file.Class), wide, 28), x, y + 56, 28, Muted, UiWeight.Bold);
                    UiShapes.Fill(new Rectangle(x, y + 100, wide, 3), 1.5f, Rule);
                    if (file.Team() is { } team) IconRow(team.Members.Select(m => (m.Species, m.Form, m.Level)), x - 8, y + 130, wide);
                    Tag(x, y + 330, Text.Of("battleRoom.ready"), Green, 44);
                    DrawWrapped(Text.Of("friends.fightBlurb", ("level", TrainerFile.FlatLevel)), x, y + 400, wide, 30, Ink, 42);
                }
                else if (entry.Refused is { } why)
                {
                    UiFonts.Draw(Fit(entry.FileName.Replace(TrainerFile.Ending, ""), wide, 44), x, y, 44, Ink, UiWeight.Black);
                    UiShapes.Fill(new Rectangle(x, y + 80, wide, 3), 1.5f, Rule);
                    Tag(x, y + 120, Text.Of("friends.refused"), Red, 44);
                    DrawWrapped(Text.Of(why.Key, ("value", why.Value)), x, y + 190, wide, 30, Ink, 42);
                }
                break;
            }
            case FriendsRowKind.ReadCode:
            case FriendsRowKind.PasteTeam:
            {
                string key = shown.Kind == FriendsRowKind.ReadCode ? "readCode" : "pasteTeam";
                UiFonts.Draw(Text.Of($"friends.row.{key}"), x, y, 44, Ink, UiWeight.Black);
                UiShapes.Fill(new Rectangle(x, y + 72, wide, 3), 1.5f, Rule);
                DrawWrapped(Text.Of($"friends.blurb.{key}"), x, y + 100, wide, 30, Ink, 42);
                break;
            }
            default:
            {
                var team = page.Teams.Teams[shown.Index];
                UiFonts.Draw(Fit(team.Name, wide, 44), x, y, 44, Ink, UiWeight.Black);
                UiShapes.Fill(new Rectangle(x, y + 72, wide, 3), 1.5f, Rule);
                IconRow(team.Members.Select(m => (m.Species, m.Form, m.Level)), x - 8, y + 100, wide);
                if (page.Code != null)
                {
                    Label(Text.Of("friends.code"), x, y + 260);
                    DrawWrapped(page.Code, x, y + 292, wide, 24, Ink, 32, UiWeight.ExtraBold);
                }
                else DrawWrapped(Text.Of("friends.blurb.send"), x, y + 280, wide, 30, Ink, 42);
                break;
            }
        }
        if (page.Notice is { } note)
        {
            string line = Text.Of(note.Key, ("value", note.Value));
            DrawWrapped(line, x, detail.Y + detail.Height - 130, wide, 28, note.Good ? Green : Red, 38, UiWeight.Black);
        }
    }
}
