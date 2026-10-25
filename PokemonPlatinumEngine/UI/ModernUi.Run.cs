using System;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

internal static partial class ModernUi
{
    private const float RunRowHeight = 62, RunRowPitch = 70;

    /// <summary>
    /// What a save's mark says of how it is played (plan 13 · V1): the preset's name, or "Custom"; null for Normal, which
    /// has no mark, so a game played as Platinum's looks as it always did.
    /// </summary>
    public static string? RunMark(RunRules? run) =>
        run == null || run.IsNormal ? null : Text.Of($"run.preset.{run.PresetKey}");

    /// <summary>
    /// A save's mark as its card draws it (plan 13 · V1, V5): the run's preset, or RUN OVER in red once a Nuzlocke has ended
    /// (its save still loads, to look at); null for a game played as Platinum's is, which has no mark.
    /// </summary>
    public static (string Text, Color Colour)? SaveMark(SaveData save) =>
        save.RunState is { Ended: true } ? (Text.Of("card.runEnded"), Red) : RunMark(save.Run) is { } mark ? (mark, Selection) : null;

    /// <summary>The words of a run row's name: a key of the table.</summary>
    public static string RunRowName(RunRow row) => Text.Of($"run.row.{row}");

    /// <summary>
    /// The panel of the ways to play that NEW GAME opens (plan 13 · V1; style guide, "Opening and title screen"): the
    /// preset on the first row, a row for each choice under it and START last, a line under the rows saying what the
    /// highlighted one does.
    /// </summary>
    public static void RunChoice(int sw, int sh, RunPanel panel, float appear = 1f)
    {
        Dim(sw, sh, (int)(170 * appear));
        var r = new Rectangle(sw / 2f - 600, 40 + (1f - appear) * 60f, 1200, 1000);
        Panel(r, 36);
        UiFonts.Draw(Text.Of("run.heading"), r.X + 56, r.Y + 36, 48, Ink, UiWeight.Black);
        UiFonts.Draw(Text.Of("run.sub"), r.X + 56, r.Y + 98, 26, Muted, UiWeight.ExtraBold);

        var rows = panel.Rows;
        float top = r.Y + 150;
        for (int i = 0; i < RunPanel.VisibleRows && panel.First + i < rows.Count; i++)
        {
            var row = rows[panel.First + i];
            bool selected = panel.First + i == panel.Selected;
            var rr = new Rectangle(r.X + 28, top + i * RunRowPitch, r.Width - 56, RunRowHeight);
            // The clauses of a Nuzlocke stand a little in from the rows they belong to
            float indent = row is RunRow.DupesClause or RunRow.ShinyClause or RunRow.GiftsCount ? 36 : 0;
            var ink = ListRow(rr, selected, RunRowName(row), nameX: 40 + indent, size: row == RunRow.Start ? 34 : 30);
            if (row != RunRow.Start) RowValue(rr, panel.ValueText(row), selected ? Color.White : row == RunRow.Preset ? Selection : ink, 30);
        }
        if (rows.Count > RunPanel.VisibleRows)
        {
            // Where the window is, when there are more rows than fit: a thin track at the panel's right edge with a thumb
            float track = RunPanel.VisibleRows * RunRowPitch - (RunRowPitch - RunRowHeight);
            UiShapes.Fill(new Rectangle(r.X + r.Width - 20, top, 8, track), 4, Rule);
            float thumb = track * RunPanel.VisibleRows / rows.Count;
            UiShapes.Fill(new Rectangle(r.X + r.Width - 20, top + (track - thumb) * panel.First / (rows.Count - RunPanel.VisibleRows), 8, thumb), 4, Muted);
        }

        UiShapes.Fill(new Rectangle(r.X + 56, r.Y + r.Height - 236, r.Width - 112, 3), 1.5f, Rule);
        DrawWrapped(Text.Of($"run.help.{panel.SelectedRow}"), r.X + 56, r.Y + r.Height - 214, r.Width - 112, 28, Muted, 38, UiWeight.ExtraBold);

        Hints(r.X + r.Width - 40, r.Y + r.Height - 78, Hint("shared.keys.leftRight", "shared.hints.change"), Hint("shared.keys.z", "run.hint.start"),
            Hint("shared.keys.esc", "shared.hints.back"));
    }
}
