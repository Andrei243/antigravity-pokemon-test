using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The memo pad (<c>memo_pad/main.c</c>): a page to write on with a pen or rub out with an eraser, the two
/// tools one above the other at the right, the eraser on top (<c>Init</c>'s hit table), the pen chosen as it opens.
/// The page starts blank every time the app comes up: the original keeps nothing of it.
///
/// The original's page is 78 by 75 dots of two pixels, drawn along the stylus's stroke, and its eraser rubs out
/// four dots by four round each point of the stroke (<c>ErasePixelsAroundPoint</c>). Here the cursor touches one
/// block at a time, so the page is a grid of <see cref="Width"/> by <see cref="Height"/> blocks (a dot of the
/// original is about half a block): the pen fills the block touched, and the eraser rubs out two blocks by two, the
/// original's four dots at this size, reaching up and left from the block touched as the original's reaches two
/// dots up and left of its point and one down and right (style guide, "The Pokétch"; plan 06 · R14b's rulings).
/// </summary>
public sealed class MemoPadApp : PoketchAppState
{
    public const int Width = PoketchCanvas.Width, Height = PoketchCanvas.Height;

    /// <summary>The tools, by the original's numbers: the eraser above, the pen below.</summary>
    public const int Eraser = 0, Pen = 1;

    /// <summary>The eraser's reach in blocks (<c>ERASER_SIZE</c>, four dots of the original).</summary>
    public const int EraserSize = 2;

    public static readonly PoketchButton EraserButton = new(Eraser, 38, 3, 6, 13);
    public static readonly PoketchButton PenButton = new(Pen, 38, 21, 6, 13);

    private static readonly IReadOnlyList<PoketchButton> buttons = PoketchCanvas.WithTools(PenButton, EraserButton);

    private readonly bool[,] dots = new bool[Width, Height];

    public override PoketchApp App => PoketchApp.MemoPad;

    /// <summary>Whether the pen is the tool in hand (the eraser otherwise).</summary>
    public bool PenActive { get; private set; } = true;

    /// <summary>Whether a block of the page is written on.</summary>
    public bool Dot(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height && dots[x, y];

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => buttons;

    public override void Press(int button, PoketchContext context)
    {
        if (button is Eraser or Pen)
        {
            // ButtonCallback: only the tool not in hand changes anything, and Task_ChangeActiveDrawingTool clicks
            if ((PenActive && button == Eraser) || (!PenActive && button == Pen))
            {
                PenActive = !PenActive;
                context.Sound("poketch");
            }
            return;
        }
        if (PoketchCanvas.CellOf(button) is not { } cell) return;
        var (x, y) = cell;
        if (PenActive) dots[x, y] = true;
        else
        {
            // ErasePixelsAroundPoint, at the page's size: from half the eraser up and left of the point, to it
            int left = Math.Max(0, x - EraserSize / 2), top = Math.Max(0, y - EraserSize / 2);
            int right = Math.Min(Width, x + EraserSize / 2), bottom = Math.Min(Height, y + EraserSize / 2);
            for (int i = left; i < right; i++)
                for (int j = top; j < bottom; j++)
                    dots[i, j] = false;
        }
    }
}

/// <summary>
/// The page the memo pad and the roulette are drawn on, in blocks: <see cref="Width"/> by <see cref="Height"/> at
/// the screen's top left, a block in from its edges, each block a button of its own numbered from
/// <see cref="CellBase"/> row by row, with the app's tools in the column on the right.
/// </summary>
internal static class PoketchCanvas
{
    public const int Left = 1, Top = 1, Width = 36, Height = 35, CellBase = 100;

    public static int IdOf(int x, int y) => CellBase + y * Width + x;

    public static (int X, int Y)? CellOf(int button)
    {
        int i = button - CellBase;
        if (i < 0 || i >= Width * Height) return null;
        return (i % Width, i / Width);
    }

    /// <summary>The tools first (so the cursor starts on the first of them), then every block of the page.</summary>
    public static IReadOnlyList<PoketchButton> WithTools(params PoketchButton[] tools)
    {
        var list = new List<PoketchButton>(tools);
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                list.Add(new PoketchButton(IdOf(x, y), Left + x, Top + y, 1, 1));
        return list;
    }
}
