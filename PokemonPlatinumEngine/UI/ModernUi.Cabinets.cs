using System;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models.Cabinets;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The arcade's two cabinets on screen (plan 42 · PS6; style guide, "The arcade"). Every piece is ours: a cabinet's
/// screen in the arcade's violet and cyan, the match-three's board of the four species' menu icons on tiles of their
/// colour, the picture-logic grid with its clues and the silhouette revealed in the species' own sprite. The rectangles
/// are worked out by plain methods, which the screens' pointer asks too.
/// </summary>
internal static partial class ModernUi
{
    private static readonly Color ArcadeTop = new(38, 22, 70, 255), ArcadeBottom = new(14, 10, 34, 255);
    private static readonly Color ArcadeCyan = new(70, 214, 230, 255), ArcadeMagenta = new(232, 84, 178, 255);
    private static readonly Color ArcadeGrid = new(250, 248, 242, 255), ArcadeFill = new(44, 40, 78, 255);

    /// <summary>
    /// A piece's tile: its species' first type's colour, softened toward white so the icon reads on it; a second piece of
    /// a type already on the board takes the colour darkened instead, so no two tiles look alike.
    /// </summary>
    private static Color MatchTileColour(MatchThree board, int piece)
    {
        var type = board.TypeOf(piece);
        int before = 0;
        for (int k = 0; k < piece; k++) if (board.TypeOf(k) == type) before++;
        var colour = Palette.GetTypeColor(type.ToString());
        return before == 0 ? Lighter(colour, 0.25f) : before == 1 ? Darker(colour, 0.25f) : Lighter(colour, 0.6f);
    }

    public const float MatchCell = 118, MatchGap = 8, MatchLeft = 140, MatchTop = 150;

    /// <summary>Where cell <paramref name="i"/> of the match-three board stands (0 to 35, along each row from the top left).</summary>
    public static Rectangle MatchCellRect(int i)
    {
        int row = i / MatchThree.Side, column = i % MatchThree.Side;
        const float pitch = MatchCell + MatchGap;
        return new Rectangle(MatchLeft + column * pitch, MatchTop + row * pitch, MatchCell, MatchCell);
    }

    public static Rectangle MatchPanelRect() => new(1000, MatchTop, 800, 744);

    /// <summary>The arcade's back: violet falling to night, a row of lights along its top.</summary>
    private static void ArcadeBack(int sw, int sh, float age)
    {
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, ArcadeTop, ArcadeBottom);
        for (int i = 0; i < 32; i++)
        {
            bool lit = ((int)(age * 5f) + i) % 3 == 0;
            UiShapes.Circle(new Vector2(30 + i * 60, 26), 8, lit ? (i % 2 == 0 ? ArcadeCyan : ArcadeMagenta) : new Color(70, 52, 104, 255));
        }
    }

    public static void DrawMatchThree(int sw, int sh, MatchThreeScreen screen, float appear)
    {
        var board = screen.Board;
        if (board == null) return;
        ArcadeBack(sw, sh, screen.Age);
        float drop = (1f - UiMotion.EaseOut(appear)) * 60f;

        // ---- the board
        const float pitch = MatchCell + MatchGap;
        var frame = new Rectangle(MatchLeft - 24, MatchTop - 24 + drop, pitch * MatchThree.Side - MatchGap + 48, pitch * MatchThree.Side - MatchGap + 48);
        UiShapes.Shadow(frame, 28, 24, new Vector2(0, 10), ShadowColor);
        UiShapes.Shape(frame, 28, ArcadeFill, Darker(ArcadeFill, 0.3f), ArcadeCyan, 5);
        bool flashing = screen.ClearAge < MatchThreeScreen.ClearTime;
        for (int i = 0; i < MatchThree.Cells; i++)
        {
            var r = ArcadeMoved(MatchCellRect(i), drop);
            int piece = board[i % MatchThree.Side, i / MatchThree.Side];
            var colour = MatchTileColour(board, piece);
            float pop = flashing ? 1f - screen.ClearAge / MatchThreeScreen.ClearTime : 0f;
            UiShapes.Shape(r, 18, Lighter(colour, 0.25f + 0.3f * pop), colour, Darker(colour, 0.35f), 4);
            var icon = PixelArtGenerator.GetPokemonIcon(board.Species[piece]);
            float s = 2f, w = icon.Width * s, h = icon.Height * s;
            Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height),
                new Rectangle(MathF.Round(r.X + r.Width / 2f - w / 2f), MathF.Round(r.Y + r.Height / 2f - h / 2f), w, h), Vector2.Zero, 0f, Color.White);
        }
        if (screen.Picked >= 0)
        {
            var p = ArcadeMoved(MatchCellRect(screen.Picked), drop);
            UiShapes.Shape(new Rectangle(p.X - 6, p.Y - 6, p.Width + 12, p.Height + 12), 22, Color.Blank, Color.Blank, ArcadeMagenta, 7);
        }
        if (screen.Phase == MatchPhase.Board)
        {
            var c = ArcadeMoved(MatchCellRect(screen.Cursor), drop);
            UiShapes.Shape(new Rectangle(c.X - 10, c.Y - 10, c.Width + 20, c.Height + 20), 24, Color.Blank, Color.Blank, Gold, 6);
        }

        // ---- the panel: the foe, its HP, the swaps, the four with their types
        var panel = ArcadeMoved(MatchPanelRect(), drop);
        Panel(panel, 30, border: 6, frame: ArcadeCyan);
        var stage = board.Stage;
        Label(Text.Of("cabinet.match.foe"), panel.X + 40, panel.Y + 32);
        var foe = PixelArtGenerator.GetPokemonSprite(stage.Foe, false);
        Raylib.DrawTexturePro(foe, new Rectangle(0, 0, foe.Width, foe.Height), new Rectangle(panel.X + 40, panel.Y + 60, 192, 192), Vector2.Zero, 0f, Color.White);
        UiFonts.Draw(LocalNames.Species(stage.Foe), panel.X + 260, panel.Y + 70, 44, Ink, UiWeight.Black);
        float tx = panel.X + 260;
        foreach (var t in stage.FoeTypes) tx += TypePill(tx, panel.Y + 132, t) + 10;
        // HP bar
        var bar = new Rectangle(panel.X + 260, panel.Y + 190, panel.Width - 300, 30);
        UiShapes.Fill(bar, 15, new Color(220, 216, 230, 255));
        float share = stage.Hp > 0 ? board.Hp / (float)stage.Hp : 0f;
        if (share > 0f) UiShapes.Fill(new Rectangle(bar.X, bar.Y, MathF.Max(30f, bar.Width * share), bar.Height), 15, share > 0.5f ? Green : share > 0.2f ? Gold : Red);
        UiFonts.Draw(Text.Of("cabinet.match.hp", ("hp", board.Hp), ("most", stage.Hp)), bar.X, bar.Y + 40, 28, Ink, UiWeight.Bold);
        Label(Text.Of("cabinet.match.swaps"), panel.X + 40, panel.Y + 300);
        UiFonts.DrawDisplay(board.SwapsLeft.ToString(), panel.X + 40, panel.Y + 326, 88, 0, Ink);
        Label(Text.Of("cabinet.match.score"), panel.X + 300, panel.Y + 300);
        UiFonts.Draw(board.Score.ToString("N0"), panel.X + 300, panel.Y + 334, 44, Darker(ArcadeMagenta, 0.2f), UiWeight.Black);
        Label(Text.Of("cabinet.match.team"), panel.X + 40, panel.Y + 452);
        for (int k = 0; k < MatchThree.Pieces; k++)
        {
            var slot = new Rectangle(panel.X + 40 + k * 184, panel.Y + 482, 168, 168);
            var tile = MatchTileColour(board, k);
            UiShapes.Shape(slot, 20, Lighter(tile, 0.3f), tile, Darker(tile, 0.35f), 4);
            var icon = PixelArtGenerator.GetPokemonIcon(board.Species[k]);
            Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height), new Rectangle(slot.X + 36, slot.Y + 10, 96, 96), Vector2.Zero, 0f, Color.White);
            float effect = board.EffectOf(k);
            string mark = effect >= 2f ? "×" + effect.ToString("0") : effect <= 0f ? "×0" : effect < 1f ? "×½" : "×1";
            UiFonts.DrawCentered(mark, slot.X + slot.Width / 2f - UiFonts.Measure(mark, 32, UiWeight.Black) / 2f, slot.Y + 132, 32, Color.White, UiWeight.Black);
        }
        Hints(sw - Margin, 1040, Hint("shared.keys.z", "cabinet.match.hint.pick"), Hint("shared.keys.esc", "cabinet.hint.quit"));

        // ---- the line
        var line = new Rectangle(MatchLeft - 24, 940, 1660, 70);
        UiShapes.Fill(line, 22, new Color(14, 10, 34, 230));
        DrawWrapped(screen.Line, line.X + 28, line.Y + 16, line.Width - 56, 30, Color.White, 36, UiWeight.ExtraBold);

        switch (screen.Phase)
        {
            case MatchPhase.Intro:
                Prompt(sw, sh, Text.Of("cabinet.match.ask.title", ("name", LocalNames.Species(stage.Foe))),
                    Text.Of("cabinet.match.ask.body", ("hp", stage.Hp), ("swaps", stage.Swaps), ("coins", stage.Coins)),
                    new[] { (Text.Of("cabinet.ask.play"), Green), (Text.Of("cabinet.ask.leave"), Blue) }, screen.Choice);
                break;
            case MatchPhase.ConfirmQuit:
                Prompt(sw, sh, Text.Of("cabinet.quitAsk.title"), Text.Of("cabinet.quitAsk.body"),
                    new[] { (Text.Of("cabinet.quitAsk.yes"), Red), (Text.Of("cabinet.quitAsk.no"), Blue) }, screen.Choice);
                break;
        }
    }

    // ------------------------------------------------------------------ the picture-logic grid

    /// <summary>The grid's whole side (the cells shrink as the grid grows), and its top left.</summary>
    public const float PictureSide = 680, PictureLeft = 460, PictureTop = 255;

    /// <summary>Where cell (<paramref name="x"/>, <paramref name="y"/>) of a grid of <paramref name="size"/> stands.</summary>
    public static Rectangle PictureCellRect(int size, int x, int y)
    {
        float cell = MathF.Floor(PictureSide / size);
        return new Rectangle(PictureLeft + x * cell, PictureTop + y * cell, cell, cell);
    }

    public static void DrawPictureLogic(int sw, int sh, PictureLogicScreen screen, float appear)
    {
        ArcadeBack(sw, sh, screen.Age);
        float drop = (1f - UiMotion.EaseOut(appear)) * 60f;
        var puzzle = screen.Puzzle;
        var line = new Rectangle(140, 962, 1640, 64);
        if (puzzle == null)
        {
            var card = ArcadeMoved(new Rectangle(sw / 2f - 420, sh / 2f - 140, 840, 280), drop);
            Panel(card, 30, border: 6, frame: ArcadeCyan);
            UiFonts.Draw(Text.Of("cabinet.picture.title"), card.X + 40, card.Y + 40, 44, Ink, UiWeight.Black);
            DrawWrapped(screen.Line, card.X + 40, card.Y + 120, card.Width - 80, 30, Ink, 38, UiWeight.Bold);
            return;
        }
        int size = puzzle.Size;
        float cellSize = MathF.Floor(PictureSide / size);
        float gridSide = cellSize * size;
        var grid = ArcadeMoved(new Rectangle(PictureLeft, PictureTop, gridSide, gridSide), drop);
        UiShapes.Shadow(new Rectangle(grid.X - 16, grid.Y - 16, grid.Width + 32, grid.Height + 32), 20, 24, new Vector2(0, 10), ShadowColor);
        UiShapes.Fill(new Rectangle(grid.X - 16, grid.Y - 16, grid.Width + 32, grid.Height + 32), 20, ArcadeGrid);
        bool solved = screen.Phase == PicturePhase.Solved;
        float reveal = solved ? Math.Clamp((screen.Age - screen.SolvedAge) / 0.8f, 0f, 1f) : 0f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var r = ArcadeMoved(PictureCellRect(size, x, y), drop);
                var inner = new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
                switch (puzzle[x, y])
                {
                    case CellMark.Filled:
                        Raylib.DrawRectangleRec(inner, ArcadeFill);
                        break;
                    case CellMark.Crossed:
                        Raylib.DrawRectangleRec(inner, new Color(236, 232, 226, 255));
                        UiShapes.Line(new Vector2(inner.X + inner.Width * 0.25f, inner.Y + inner.Height * 0.25f),
                            new Vector2(inner.X + inner.Width * 0.75f, inner.Y + inner.Height * 0.75f), MathF.Max(2f, cellSize / 12f), Red);
                        UiShapes.Line(new Vector2(inner.X + inner.Width * 0.75f, inner.Y + inner.Height * 0.25f),
                            new Vector2(inner.X + inner.Width * 0.25f, inner.Y + inner.Height * 0.75f), MathF.Max(2f, cellSize / 12f), Red);
                        break;
                    default:
                        Raylib.DrawRectangleRec(inner, Color.White);
                        break;
                }
            }
        // Heavier lines every fifth cell
        for (int k = 0; k <= size; k += 5)
        {
            Raylib.DrawRectangleRec(new Rectangle(grid.X + k * cellSize - 1.5f, grid.Y, 3, gridSide), ArcadeTop);
            Raylib.DrawRectangleRec(new Rectangle(grid.X, grid.Y + k * cellSize - 1.5f, gridSide, 3), ArcadeTop);
        }
        // The species in its own colours over the grid as it is solved
        if (solved)
        {
            var sprite = PixelArtGenerator.GetPokemonSprite(puzzle.Species, false);
            Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height), grid, Vector2.Zero, 0f,
                new Color(255, 255, 255, (int)(235 * reveal)));
        }
        else if (screen.Phase == PicturePhase.Board)
        {
            var c = ArcadeMoved(PictureCellRect(size, screen.CursorX, screen.CursorY), drop);
            UiShapes.Shape(new Rectangle(c.X - 3, c.Y - 3, c.Width + 6, c.Height + 6), 6, Color.Blank, Color.Blank, ArcadeMagenta, 4);
        }

        // The clues: each row's runs at its left, each column's above it, grey once the line is done
        float clueSize = Math.Clamp(cellSize * 0.55f, 14f, 34f);
        for (int i = 0; i < size; i++)
        {
            var rowColour = puzzle.LineDone(true, i) ? new Color(170, 160, 200, 255) : Color.White;
            string rowText = puzzle.RowClues[i].Length == 0 ? "0" : string.Join(" ", puzzle.RowClues[i]);
            float rw = UiFonts.Measure(rowText, clueSize, UiWeight.Black);
            UiFonts.DrawCentered(rowText, grid.X - 28 - rw, grid.Y + i * cellSize + cellSize / 2f, clueSize, rowColour, UiWeight.Black);
            var colColour = puzzle.LineDone(false, i) ? new Color(170, 160, 200, 255) : Color.White;
            var runs = puzzle.ColumnClues[i];
            int count = Math.Max(1, runs.Length);
            for (int k = 0; k < count; k++)
            {
                string t = runs.Length == 0 ? "0" : runs[k].ToString();
                float tw = UiFonts.Measure(t, clueSize, UiWeight.Black);
                float yy = grid.Y - 30 - (count - 1 - k) * clueSize * 1.05f;
                UiFonts.DrawCentered(t, grid.X + i * cellSize + cellSize / 2f - tw / 2f, yy, clueSize, colColour, UiWeight.Black);
            }
        }

        // ---- the panel
        var panel = ArcadeMoved(new Rectangle(1320, PictureTop, 480, 420), drop);
        Panel(panel, 30, border: 6, frame: ArcadeCyan);
        Label(Text.Of("cabinet.picture.title"), panel.X + 32, panel.Y + 30);
        UiFonts.Draw(solved ? LocalNames.Species(puzzle.Species) : Text.Of("cabinet.picture.unknown"), panel.X + 32, panel.Y + 62, 40, Ink, UiWeight.Black);
        Label(Text.Of("cabinet.picture.grid"), panel.X + 32, panel.Y + 140);
        UiFonts.Draw($"{size} × {size}", panel.X + 32, panel.Y + 168, 40, Ink, UiWeight.Black);
        Label(Text.Of("cabinet.picture.mistakes"), panel.X + 32, panel.Y + 246);
        UiFonts.Draw(puzzle.Mistakes.ToString(), panel.X + 32, panel.Y + 274, 40, puzzle.Mistakes > 0 ? Red : Ink, UiWeight.Black);
        Label(Text.Of("cabinet.picture.filled"), panel.X + 240, panel.Y + 246);
        UiFonts.Draw($"{puzzle.FilledCount}/{puzzle.PictureCount}", panel.X + 240, panel.Y + 274, 40, Ink, UiWeight.Black);
        Hints(sw - Margin, 1040, Hint("shared.keys.z", "cabinet.picture.hint.fill"), Hint("shared.keys.c", "cabinet.picture.hint.cross"),
            Hint("shared.keys.esc", "cabinet.hint.quit"));

        UiShapes.Fill(line, 22, new Color(14, 10, 34, 230));
        DrawWrapped(screen.Line, line.X + 28, line.Y + 16, line.Width - 56, 30, Color.White, 36, UiWeight.ExtraBold);

        if (screen.Phase == PicturePhase.ConfirmQuit)
            Prompt(sw, sh, Text.Of("cabinet.quitAsk.title"), Text.Of("cabinet.quitAsk.body"),
                new[] { (Text.Of("cabinet.quitAsk.yes"), Red), (Text.Of("cabinet.quitAsk.no"), Blue) }, screen.Choice);
    }

    private static Rectangle ArcadeMoved(Rectangle r, float dy) => new(r.X, r.Y + dy, r.Width, r.Height);
}
