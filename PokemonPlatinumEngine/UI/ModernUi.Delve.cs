using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Delve;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The delve's panels over its floor and the camp's board (plan 44 · SD8, SD9; style guide, "The delve").</summary>
internal static partial class ModernUi
{
    /// <summary>The delve's panels: the floor's pill, the team's cards and the hunger, the log, a list, the hints.</summary>
    public static void DrawDelve(int sw, int sh, DelveScreen screen)
    {
        if (screen.Run is not { } run) return;

        // The hollow and its floor, top left
        string place = DelveBoardScreen.HollowName(run.Hollow.Key);
        string floor = Text.Of("delve.floor", ("n", run.Floor.Index + 1), ("of", run.Hollow.Floors));
        float pw = Math.Max(UiFonts.Measure(place, 30, UiWeight.Black), UiFonts.Measure(floor, 22, UiWeight.ExtraBold)) + 56;
        var top = new Rectangle(32, 32, pw, 92);
        Panel(top, 24);
        UiFonts.Draw(place, top.X + 28, top.Y + 14, 30, Ink, UiWeight.Black);
        UiFonts.Draw(floor, top.X + 28, top.Y + 54, 22, Muted, UiWeight.ExtraBold);

        // The team's cards, down the left
        float y = 144;
        for (int i = 0; i < run.Team.Count; i++)
        {
            var m = run.Team[i];
            var card = new Rectangle(32, y, 360, 92);
            Panel(card, 20, frame: i == 0 ? Selection : null);
            Portrait(new Vector2(card.X + 48, card.Y + 46), 34, m.Pokemon, 2, i == 0);
            UiFonts.Draw(m.Pokemon.DisplayName, card.X + 96, card.Y + 12, 26, Ink, UiWeight.Black);
            string tag = i == 0 ? Text.Of("delve.leader") : m.Recruit ? Text.Of("delve.recruit") : Text.Of("delve.partner");
            UiFonts.Draw(Text.Of("delve.level", ("n", m.Pokemon.Level)) + "  " + tag, card.X + 96, card.Y + 42, 18, Muted, UiWeight.ExtraBold);
            HpBar(card.X + 96, card.Y + 68, 240, 12, m.Pokemon.MaxHP == 0 ? 0f : (float)m.Pokemon.CurrentHP / m.Pokemon.MaxHP);
            y += 104;
        }

        // The hunger, under the cards
        var belly = new Rectangle(32, y, 360, 64);
        Panel(belly, 20);
        Label(Text.Of("delve.hunger"), belly.X + 24, belly.Y + 10);
        Bar(new Rectangle(belly.X + 24, belly.Y + 38, 312, 12), run.Hunger / (float)DelveRun.FullHunger, run.Hunger <= 20 ? Red : Gold);

        // The log, along the bottom
        var log = new Rectangle(424, sh - 32 - 196, sw - 424 - 32, 196);
        // (nothing said yet, nothing drawn: an empty panel only covers the floor)
        if (screen.Log.Count > 0) Panel(log, 24);
        float ly = log.Y + 22;
        for (int i = 0; i < screen.Log.Count; i++)
        {
            bool newest = i == screen.Log.Count - 1;
            UiFonts.Draw(screen.Log[i], log.X + 32, ly, 26, newest ? Ink : Muted, newest ? UiWeight.Black : UiWeight.ExtraBold);
            ly += 40;
        }

        // A list, at the right
        switch (screen.Menu)
        {
            case DelveMenu.Moves:
                DelveList(sw, Text.Of("delve.moves"), run.Leader.Pokemon.Moves.Select(DelveScreen.MoveRow).ToList(), screen.Cursor);
                break;
            case DelveMenu.Bag:
                var items = screen.BagItems;
                DelveList(sw, Text.Of("delve.bag", ("n", run.Bag.Count), ("of", DelveRun.BagSize)),
                    items.Count == 0 ? new() { Text.Of("delve.bagEmpty") } : items.Select(i => LocalNames.Item(i) + "  ×" + screen.CountOf(i)).ToList(), screen.Cursor);
                break;
            case DelveMenu.BagTarget:
                DelveList(sw, Text.Of("delve.giveTo", ("item", LocalNames.Item(screen.Chosen ?? ""))), run.Team.Select(m => m.Pokemon.DisplayName).ToList(), screen.Cursor);
                break;
            case DelveMenu.Asking when run.Asking is { } asking:
                DelveList(sw, Text.Of("delve.asks", ("pokemon", asking.Pokemon.DisplayName)), new() { Text.Of("delve.yes"), Text.Of("delve.no") }, screen.Cursor);
                break;
        }

        if (screen.Refused is { } refused && !screen.Playing && refused != DelveRefusal.Blocked)
            UiFonts.Draw(Text.Of($"delve.refused.{refused}"), log.X + 32, log.Y - 40, 24, Red, UiWeight.Black);

        if (!screen.Playing)
            Hints(sw - 32, 32,
                screen.Menu == DelveMenu.Field ? Hint("shared.keys.z", "delve.hint.strike") : Hint("shared.keys.z", "shared.hints.choose"),
                Hint("shared.keys.enter", "delve.hint.moves"),
                Hint("shared.keys.x", "shared.hints.back"));
    }

    private static void DelveList(int sw, string title, System.Collections.Generic.List<string> rows, int cursor)
    {
        float width = 560, rowH = 56;
        var r = new Rectangle(sw - 32 - width, 112, width, 96 + rows.Count * rowH);
        Panel(r, 28);
        UiFonts.Draw(title, r.X + 32, r.Y + 24, 28, Ink, UiWeight.Black);
        for (int i = 0; i < rows.Count; i++)
        {
            var row = new Rectangle(r.X + 20, r.Y + 76 + i * rowH, width - 40, rowH - 8);
            if (i == cursor) UiShapes.Fill(row, 14, Selection);
            UiFonts.Draw(rows[i], row.X + 20, row.Y + 10, 26, i == cursor ? Color.White : Ink, UiWeight.ExtraBold);
        }
    }

    /// <summary>The camp's board over the field: the day's requests, which are done, what each pays, the camp's rank.</summary>
    public static void DrawDelveBoard(int sw, int sh, DelveBoardScreen screen)
    {
        if (!screen.IsActive) return;
        Dim(sw, sh, 120);
        float width = 1200, rowH = 132;
        var r = new Rectangle(sw / 2f - width / 2f, 140, width, 184 + Math.Max(1, screen.Requests.Count) * rowH);
        Panel(r, 36);
        UiFonts.Draw(Text.Of("delve.board"), r.X + 48, r.Y + 36, 44, Ink, UiWeight.Black);
        string rank = Text.Of("delve.rank", ("n", screen.Rank));
        UiFonts.Draw(rank, r.X + width - 48 - UiFonts.Measure(rank, 26, UiWeight.ExtraBold), r.Y + 48, 26, Muted, UiWeight.ExtraBold);
        if (screen.Requests.Count == 0)
            UiFonts.Draw(Text.Of("delve.boardEmpty"), r.X + 48, r.Y + 120, 28, Muted, UiWeight.ExtraBold);
        for (int i = 0; i < screen.Requests.Count; i++)
        {
            var q = screen.Requests[i];
            bool done = i < screen.Done.Count && screen.Done[i];
            var row = new Rectangle(r.X + 32, r.Y + 112 + i * rowH, width - 64, rowH - 16);
            UiShapes.Fill(row, 18, i == screen.Cursor ? new Color(255, 236, 228, 255) : new Color(240, 244, 250, 255));
            UiFonts.Draw(DelveBoardScreen.LineOf(q), row.X + 28, row.Y + 18, 28, done ? Muted : Ink, UiWeight.Black);
            UiFonts.Draw(Text.Of("delve.pays", ("prize", DelveBoardScreen.PrizeOf(q))), row.X + 28, row.Y + 66, 22, Muted, UiWeight.ExtraBold);
            if (done)
            {
                string mark = Text.Of("delve.done");
                UiFonts.Draw(mark, row.X + row.Width - 28 - UiFonts.Measure(mark, 26, UiWeight.Black), row.Y + 40, 26, Green, UiWeight.Black);
            }
        }
        Hints(r.X + width - 32, r.Y + r.Height - 64, Hint("shared.keys.z", "shared.hints.back"));
    }
}
