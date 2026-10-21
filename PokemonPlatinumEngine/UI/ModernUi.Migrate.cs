using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Foreign;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The migration's screen (plan 08 · P21; style guide, "Menu screens", "Migrate").</summary>
internal static partial class ModernUi
{
    private const float MigrateCell = 112, MigrateGap = 10;

    public static void DrawMigrate(int sw, int sh, MigrateScreen screen, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("migrate.title"));
        switch (screen.Phase)
        {
            case MigratePhase.Menu:
                Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("shared.keys.z", "shared.hints.open"), Hint("shared.keys.esc", "shared.hints.back"));
                MigrateMenu(sw, screen, appear);
                break;
            case MigratePhase.Save:
                Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("shared.keys.z", "shared.hints.open"), Hint("shared.keys.esc", "shared.hints.back"));
                MigrateSaves(sw, screen, appear);
                break;
            case MigratePhase.Files:
                Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("shared.keys.z", "shared.hints.open"), Hint("shared.keys.esc", "shared.hints.back"));
                MigrateFiles(sw, screen, appear);
                break;
            default:
                if (screen.Mode == MigrateMode.WriteOut)
                    Hints(sw - Margin, 44, Hint("shared.keys.arrows", "shared.hints.choose"), Hint("shared.keys.z", "shared.hints.pick"),
                        Hint("migrate.keyBoxes", "shared.hints.changeBox"), Hint("shared.keys.esc", "shared.hints.back"));
                else
                    Hints(sw - Margin, 44, Hint("shared.keys.arrows", "shared.hints.choose"), Hint("shared.keys.z", "shared.hints.pick"),
                        Hint("migrate.keyBoxes", "shared.hints.changeBox"), Hint("shared.keys.enter", "migrate.hints.bring"), Hint("shared.keys.esc", "shared.hints.back"));
                MigrateBox(sw, screen, appear);
                break;
        }

        if (screen.Phase == MigratePhase.Format)
            Prompt(sw, sh, Text.Of("migrate.out.title", ("name", screen.ToWrite != null ? MigrateName(screen.ToWrite) : "")), Text.Of("migrate.out.body"),
                new[] { (Text.Of("migrate.out.pk4"), screen.ToWrite != null && MigrateScreen.CanBePk4(screen.ToWrite) ? Blue : Muted), (Text.Of("migrate.out.showdown"), Green) },
                screen.Format == WriteFormat.Pk4 ? 0 : 1);
        else if (screen.Phase == MigratePhase.Confirm)
            Prompt(sw, sh, Text.Of("migrate.confirm.title", ("n", Migration.Size)), Text.Of(screen.Source?.Slot != null ? "migrate.confirm.bodyOwn" : "migrate.confirm.body", ("who", screen.Source?.Name ?? "")),
                new[] { (Text.Of("migrate.confirm.no"), Blue), (Text.Of("migrate.confirm.yes"), Green) }, screen.ConfirmYes ? 1 : 0);
        else if (screen.Phase == MigratePhase.Working)
            Prompt(sw, sh, Text.Of("migrate.working.title"), Text.Of("migrate.working.body"), Array.Empty<(string, Color)>(), -1);
        else if (screen.Phase == MigratePhase.Result && screen.Mode == MigrateMode.WriteOut)
            Notice(sw, sh, Text.Of(screen.Wrote != null ? "migrate.out.done.title" : "migrate.out.failed.title"),
                screen.Wrote != null ? Text.Of("migrate.out.done.body", ("file", screen.Wrote)) : Text.Of("migrate.out.failed.body"), Text.Of("migrate.result.ok"));
        else if (screen.Phase == MigratePhase.Result)
        {
            bool done = screen.Outcome == MigrationBlock.None;
            Notice(sw, sh, Text.Of(done ? "migrate.result.done.title" : "migrate.result.refused.title"),
                done ? Text.Of("migrate.result.done.body", ("n", Migration.Size)) : Migration.Words(screen.Outcome), Text.Of("migrate.result.ok"));
        }
    }

    // ------------------------------------------------------------------ what to do

    private static void MigrateMenu(int sw, MigrateScreen screen, float appear)
    {
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var list = new Rectangle(Margin, ContentTop + rise, 1040, ContentBottom - ContentTop);
        Panel(list, 40);
        string[] rows = { Text.Of("migrate.menu.bring"), Text.Of("migrate.menu.out") };
        for (int row = 0; row < rows.Length; row++)
            ListRow(RowRect(list, row), row == screen.Cursor, rows[row], nameX: 44);

        var detail = new Rectangle(list.X + list.Width + Gutter, ContentTop + rise, sw - Margin * 2 - list.Width - Gutter, ContentBottom - ContentTop);
        Panel(detail, 40);
        float x = detail.X + 44, wide = detail.Width - 88, y = detail.Y + 48;
        UiFonts.Draw(rows[screen.Cursor], x, y, 40, Ink, UiWeight.Black);
        UiShapes.Fill(new Rectangle(x, y + 64, wide, 3), 1.5f, Rule);
        DrawWrapped(Text.Of(screen.Cursor == 0 ? "migrate.menu.bringBody" : "migrate.menu.outBody"), x, y + 96, wide, 30, Ink, 42);
    }

    // ------------------------------------------------------------------ the saves

    private static void MigrateSaves(int sw, MigrateScreen screen, float appear)
    {
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var list = new Rectangle(Margin, ContentTop + rise, 1040, ContentBottom - ContentTop);
        Panel(list, 40);
        int visible = MigrateScreen.VisibleRows;
        for (int row = 0; row < visible && screen.FirstRow + row < screen.Targets.Count; row++)
        {
            int index = screen.FirstRow + row;
            var target = screen.Targets[index];
            var r = RowRect(list, row, scrolls: screen.Targets.Count > visible);
            bool selected = index == screen.Cursor;
            var ink = ListRow(r, selected, Fitted(target.Save.PlayerName ?? "", r.Width - 104 - 240, 34), nameX: 44);
            RowValue(r, Text.Of("migrate.slot", ("slot", target.Slot)), ink, 26);
        }
        ScrollBar(list, screen.FirstRow, visible, screen.Targets.Count);

        var detail = new Rectangle(list.X + list.Width + Gutter, ContentTop + rise, sw - Margin * 2 - list.Width - Gutter, ContentBottom - ContentTop);
        Panel(detail, 40);
        if (screen.Cursor >= screen.Targets.Count) return;
        var shown = screen.Targets[screen.Cursor];
        float x = detail.X + 44, wide = detail.Width - 88, y = detail.Y + 48;
        UiFonts.Draw(Fitted(shown.Save.PlayerName ?? "", wide, 44), x, y, 44, Ink, UiWeight.Black);
        Tag(x, y + 70, shown.Save.NationalPokedex ? Text.Of("migrate.national.yes") : Text.Of("migrate.national.no"), shown.Save.NationalPokedex ? Green : Red, 44);
        UiShapes.Fill(new Rectangle(x, y + 142, wide, 3), 1.5f, Rule);
        var park = screen.ParkOf(shown);
        Label(Text.Of("migrate.parkLabel"), x, y + 172);
        DrawWrapped(park.Waiting.Count > 0 ? Text.Of("migrate.park.waiting", ("n", park.Waiting.Count)) : Text.Of("migrate.park.empty"),
            x, y + 204, wide, 30, Ink, 42);
    }

    // ------------------------------------------------------------------ the files

    private static void MigrateFiles(int sw, MigrateScreen screen, float appear)
    {
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var list = new Rectangle(Margin, ContentTop + rise, 1040, ContentBottom - ContentTop);
        Panel(list, 40);
        if (screen.Sources.Count == 0)
        {
            EmptyNote(list, Text.Of("migrate.empty"));
            return;
        }
        int visible = MigrateScreen.VisibleRows;
        for (int row = 0; row < visible && screen.FirstRow + row < screen.Sources.Count; row++)
        {
            int index = screen.FirstRow + row;
            var source = screen.Sources[index];
            var r = RowRect(list, row, scrolls: screen.Sources.Count > visible);
            bool selected = index == screen.Cursor;
            bool open = source.Readable && screen.BlockOf(source) == MigrationBlock.None;
            var ink = ListRow(r, selected, Fitted(source.Name, r.Width - 104 - 300, 34), open, nameX: 44);
            RowValue(r, source.Readable ? source.Kind : Text.Of("migrate.cantread"), open ? ink : selected ? ink : Red, 26);
        }
        ScrollBar(list, screen.FirstRow, visible, screen.Sources.Count);

        var detail = new Rectangle(list.X + list.Width + Gutter, ContentTop + rise, sw - Margin * 2 - list.Width - Gutter, ContentBottom - ContentTop);
        Panel(detail, 40);
        if (screen.Cursor >= screen.Sources.Count) return;
        var shown = screen.Sources[screen.Cursor];
        float x = detail.X + 44, wide = detail.Width - 88, y = detail.Y + 48;
        UiFonts.Draw(Fitted(shown.Name, wide, 40), x, y, 40, Ink, UiWeight.Black);
        if (shown.Save is not { } save)
        {
            Tag(x, y + 66, Text.Of("migrate.cantread"), Red, 44);
            UiShapes.Fill(new Rectangle(x, y + 138, wide, 3), 1.5f, Rule);
            DrawWrapped(shown.Notice, x, y + 170, wide, 30, Ink, 42);
            return;
        }
        UiFonts.Draw(shown.Kind, x, y + 56, 28, Muted, UiWeight.ExtraBold);
        UiShapes.Fill(new Rectangle(x, y + 108, wide, 3), 1.5f, Rule);
        float line = y + 138;
        if (save.TrainerName.Length > 0)
        {
            Label(Text.Of("migrate.trainer"), x, line);
            UiFonts.Draw(Fitted(save.TrainerName, wide, 36), x, line + 28, 36, Ink, UiWeight.Black);
            line += 92;
        }
        Label(Text.Of("migrate.pokemonLabel"), x, line);
        UiFonts.Draw(Text.Of("migrate.pokemon.count", ("n", save.Count)), x, line + 28, 36, Ink, UiWeight.Black);
        line += 92;
        var block = screen.BlockOf(shown);
        UiShapes.Fill(new Rectangle(x, line, wide, 3), 1.5f, Rule);
        if (block == MigrationBlock.None) Tag(x, line + 32, Text.Of("migrate.ready"), Green, 44);
        else
        {
            Tag(x, line + 32, Text.Of("migrate.waits"), Red, 44);
            DrawWrapped(screen.BlockWords(shown), x, line + 100, wide, 30, Ink, 42);
        }
    }

    // ------------------------------------------------------------------ a file's boxes

    /// <summary>What a Pokémon of a source is called on this screen: its nickname if it has one, else its species.</summary>
    public static string MigrateName(ForeignPokemon p)
    {
        if (p.IsBadEgg) return Text.Of("migrate.badEgg");
        if (p.IsEgg) return Text.Of("migrate.egg");
        string species = p.SpeciesData is { } s ? LocalNames.Species(s.Name) : "?";
        return p.HasNickname && p.Nickname.Length > 0 && !string.Equals(p.Nickname, p.SpeciesData?.Name, StringComparison.OrdinalIgnoreCase) ? p.Nickname : species;
    }

    private static Texture2D MigrateIcon(ForeignPokemon p) => p.IsEgg || p.SpeciesData == null ? PixelArtGenerator.EggIcon : PixelArtGenerator.GetPokemonIcon(p.SpeciesData.Name);

    private static void MigrateBox(int sw, MigrateScreen screen, float appear)
    {
        bool bring = screen.Mode == MigrateMode.Bring;
        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        float height = ContentBottom - ContentTop;
        var box = new Rectangle(Margin - slide, ContentTop, 792, height);
        Panel(box, 34);

        // ---- The box's name between its arrows, and which box it is
        string name = (screen.Box?.Name ?? "").ToUpperInvariant();
        var plate = new Rectangle(box.X + box.Width / 2f - 190, box.Y + 32, 380, 76);
        UiShapes.Shadow(plate, 38, 14, new Vector2(0, 5), ShadowColor);
        UiShapes.Shape(plate, 38, SlotTop, SlotBottom, Rule, 3);
        float nameW = UiFonts.Measure(name, 40, UiWeight.Black);
        UiFonts.DrawCentered(Fitted(name, 340, 40), plate.X + (plate.Width - Math.Min(nameW, 340)) / 2f, plate.Y + plate.Height / 2f, 40, Ink, UiWeight.Black);
        if (screen.BoxCount > 1)
            for (int side = -1; side <= 1; side += 2)
            {
                var at = new Vector2(side < 0 ? plate.X - 46 : plate.X + plate.Width + 46, plate.Y + plate.Height / 2f);
                UiShapes.Circle(at, 28, new Color(226, 234, 246, 255));
                UiIcons.ArrowH(at + new Vector2(side * 2, 0), 20, side, new Color(150, 162, 186, 255));
            }
        string which = $"{screen.BoxNumber + 1} / {screen.BoxCount}";
        UiFonts.DrawCentered(which, box.X + box.Width / 2f - UiFonts.Measure(which, 22, UiWeight.Black) / 2f, plate.Y + plate.Height + 28, 22, Muted, UiWeight.Black);

        // ---- The places
        float gridX = box.X + (box.Width - (MigrateCell * MigrateScreen.Columns + MigrateGap * (MigrateScreen.Columns - 1))) / 2f, gridY = box.Y + 172;
        Rectangle CellRect(int i) => new(gridX + i % MigrateScreen.Columns * (MigrateCell + MigrateGap), gridY + i / MigrateScreen.Columns * (MigrateCell + MigrateGap), MigrateCell, MigrateCell);
        for (int i = 0; i < MigrateScreen.PerBox; i++)
        {
            var r = CellRect(i);
            bool selected = screen.Place == i;
            int number = screen.ChosenNumber(i);
            var pokemon = screen.Box != null && i < screen.Box.Slots.Count ? screen.Box.Slots[i] : null;
            if (selected)
            {
                UiShapes.Shadow(r, 24, 22, Vector2.Zero, Selection with { A = 190 });
                UiShapes.Shape(r, 24, SlotTop, SlotBottom, Selection, 6f);
            }
            else if (number > 0) UiShapes.Shape(r, 24, new Color(255, 244, 214, 255), new Color(250, 232, 184, 255), Gold, 5f);
            else UiShapes.Shape(r, 24, SlotTop, SlotBottom, Rule, 3f);
            if (pokemon == null) continue;
            bool allowed = !bring || screen.VerdictOf(pokemon).Allowed;
            PixelArt(MigrateIcon(pokemon), new Vector2(r.X + MigrateCell / 2f, r.Y + MigrateCell / 2f - 2), 2, selected && allowed ? Hop(true) * 2 : 0,
                allowed ? Color.White : new Color(150, 156, 176, 170));
            if (number > 0)
            {
                var badge = new Vector2(r.X + r.Width - 20, r.Y + 20);
                UiShapes.Circle(badge, 17, Gold);
                UiFonts.DrawCentered(number.ToString(), badge.X - UiFonts.Measure(number.ToString(), 22, UiWeight.Black) / 2f, badge.Y, 22, Color.White, UiWeight.Black);
            }
            else if (!allowed)
            {
                var cross = new Vector2(r.X + 20, r.Y + 20);
                UiShapes.Circle(cross, 15, Red);
                UiFonts.DrawCentered("!", cross.X - UiFonts.Measure("!", 22, UiWeight.Black) / 2f, cross.Y, 22, Color.White, UiWeight.Black);
            }
        }

        // ---- How many are chosen (in the way out one is chosen, and the box's own name says where it is)
        float tagY = gridY + MigrateScreen.Rows * (MigrateCell + MigrateGap) + 12;
        if (bring) Tag(gridX, tagY, Text.Of("migrate.chosen", ("n", screen.ChosenCount), ("of", Migration.Size)), screen.ChosenCount == Migration.Size ? Green : Frame with { A = 225 }, 44);

        // ---- The one under the cursor, and the six
        var detail = new Rectangle(box.X + box.Width + Gutter + slide, ContentTop, sw - Margin - (box.X + box.Width + Gutter + slide), height);
        Panel(detail, 34);
        MigrateDetail(detail, screen);
    }

    private static void MigrateDetail(Rectangle detail, MigrateScreen screen)
    {
        float x = detail.X + 40, wide = detail.Width - 80, y = detail.Y + 36;
        if (screen.Under is not { } p)
        {
            string note = Text.Of("migrate.emptyPlace");
            UiFonts.Draw(note, detail.X + (detail.Width - UiFonts.Measure(note, 26, UiWeight.ExtraBold)) / 2f, detail.Y + 220, 26, Muted, UiWeight.ExtraBold);
        }
        else
        {
            bool bring = screen.Mode == MigrateMode.Bring;
            var verdict = bring ? screen.VerdictOf(p) : new MigrationVerdict(MigrationRefusal.None);
            var disc = new Vector2(detail.X + detail.Width / 2f, y + 100);
            UiShapes.Circle(disc, 100, Disc);
            PixelArt(MigrateIcon(p), disc, 4, 0, verdict.Allowed ? Color.White : new Color(150, 156, 176, 200));
            UiFonts.DrawCentered(Fitted(MigrateName(p), wide, 40), x + (wide - Math.Min(wide, UiFonts.Measure(MigrateName(p), 40, UiWeight.Black))) / 2f, y + 236, 40, Ink, UiWeight.Black);
            if (!p.IsEgg && p.SpeciesData != null)
            {
                string line = Text.Of("migrate.levelOf", ("level", p.Level), ("species", LocalNames.Species(p.SpeciesData.Name)));
                UiFonts.Draw(Fitted(line, wide, 26), x, y + 270, 26, Muted, UiWeight.ExtraBold);
            }
            if (p.OtName.Length > 0)
            {
                Label(Text.Of("migrate.ot"), x, y + 322);
                UiFonts.Draw(Fitted(p.OtName, wide, 30), x, y + 348, 30, Ink, UiWeight.Black);
            }
            UiShapes.Fill(new Rectangle(x, y + 400, wide, 3), 1.5f, Rule);
            if (!bring)
            {
                // The way out: which of the two it can be written as
                bool pk4 = MigrateScreen.CanBePk4(p);
                Tag(x, y + 424, Text.Of(pk4 ? "migrate.out.either" : "migrate.out.textOnly"), pk4 ? Green : Gold, 44);
                if (!pk4) DrawWrapped(Text.Of("migrate.out.textOnlyWhy"), x, y + 490, wide, 28, Ink, 40);
            }
            else if (verdict.Allowed) Tag(x, y + 424, screen.ChosenNumber(screen.Place) > 0 ? Text.Of("migrate.picked") : Text.Of("migrate.canCome"), Green, 44);
            else
            {
                Tag(x, y + 424, Text.Of("migrate.cantCome"), Red, 44);
                DrawWrapped(Migration.Words(verdict), x, y + 490, wide, 28, Ink, 40);
            }
        }

        // The six, in the order they were chosen, along the panel's foot
        if (screen.Mode == MigrateMode.WriteOut) return;
        float footY = detail.Y + detail.Height - 150;
        UiShapes.Fill(new Rectangle(x, footY - 18, wide, 3), 1.5f, Rule);
        Label(Text.Of("migrate.six"), x, footY);
        var six = screen.Six;
        float step = wide / Migration.Size;
        for (int i = 0; i < Migration.Size; i++)
        {
            var c = new Vector2(x + step * i + step / 2f, footY + 70);
            UiShapes.Circle(c, 36, i < six.Count ? Disc : new Color(236, 240, 248, 255));
            if (i < six.Count) PixelArt(MigrateIcon(six[i]), c, 1);
        }
    }
}
