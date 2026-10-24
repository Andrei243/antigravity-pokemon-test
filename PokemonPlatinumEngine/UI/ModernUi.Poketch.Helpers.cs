using System;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.PoketchApps;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The Pokétch's Hidden Moves app (plan 12 · Q18; style guide, "The Pokétch").</summary>
internal static partial class ModernUi
{
    /// <summary>
    /// A heading with a rule under it and, a row to each, the field moves a wild helper does, in the LCD's ink with a
    /// block for a bullet; "NO HELPERS" in the middle tone when none is granted yet.
    /// </summary>
    private static void PoketchHiddenMoves(Rectangle screen, HiddenMovesApp app, PoketchContext context)
    {
        UiFonts.DrawCentered(Text.Of("poketch.hiddenMoves"), screen.X + 2 * Block, screen.Y + 3 * Block, 26, LcdInk, UiWeight.Black);
        LcdCells(screen, 1, 6, PoketchAppState.Columns - 2, 1, LcdInk);
        var moves = app.Moves(context);
        if (moves.Count == 0)
        {
            UiFonts.DrawCentered(Text.Of("poketch.hiddenNone"), screen.X + 2 * Block, screen.Y + 17 * Block, 24, LcdMid, UiWeight.Black);
            return;
        }
        for (int i = 0; i < Math.Min(moves.Count, HiddenMovesApp.Rows8); i++)
        {
            int row = 8 + i * 3;
            string name = LocalNames.Move(FieldMoveRules.MoveName(moves[i])).ToUpperInvariant();
            LcdCells(screen, 2, row + 1, 1, 1, LcdInk);
            UiFonts.DrawCentered(name, screen.X + 5 * Block, screen.Y + (row + 1.5f) * Block, 24, LcdInk, UiWeight.Black);
        }
    }
}
