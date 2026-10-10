using System;
using Raylib_cs;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Pokétch's toys (plan 06 · R14b; style guide, "The Pokétch"): the calculator, the memo pad, the counter, the
/// coin toss, the roulette, the dot art, the colour changer and the kitchen timer, each in the LCD's three tones and
/// blocks of 8, laid out on the buttons their states give. The art is our own.
/// </summary>
internal static partial class ModernUi
{
    // ------------------------------------------------------------------ small figures

    // The calculator's places and signs, 3 blocks by 5, a row of three bits each from the top
    private static readonly int[][] MiniGlyphs =
    {
        new[] { 7, 5, 5, 5, 7 }, new[] { 1, 1, 1, 1, 1 }, new[] { 7, 1, 7, 4, 7 }, new[] { 7, 1, 7, 1, 7 },
        new[] { 5, 5, 7, 1, 1 }, new[] { 7, 4, 7, 1, 7 }, new[] { 7, 4, 7, 5, 7 }, new[] { 7, 1, 1, 1, 1 },
        new[] { 7, 5, 7, 5, 7 }, new[] { 7, 5, 7, 1, 7 },
        new[] { 0, 0, 0, 0, 2 }, // the point
        new[] { 0, 0, 7, 0, 0 }, // minus
        new[] { 7, 4, 7, 4, 7 }, // the error
    };

    private static readonly int[] MiniPlus = { 0, 2, 7, 2, 0 }, MiniTimes = { 0, 5, 2, 5, 0 }, MiniDivide = { 2, 0, 7, 0, 2 };

    /// <summary>A figure 3 blocks by 5 at a place in blocks, lit in the ink.</summary>
    private static void MiniGlyph(Rectangle screen, int col, int row, int[] rows, Color color)
    {
        for (int r = 0; r < 5; r++)
            for (int c = 0; c < 3; c++)
                if ((rows[r] & (4 >> c)) != 0) LcdCells(screen, col + c, row + r, 1, 1, color);
    }

    // ------------------------------------------------------------------ the calculator

    /// <summary>
    /// The calculator: the operation waiting at the top left, ten places of figures 3 blocks by 5 beside it (each
    /// place's unlit cells faint), a rule of the middle tone under them, and the keys below in four rows.
    /// </summary>
    private static void PoketchCalculator(Rectangle screen, CalculatorApp app, PoketchContext context)
    {
        var faint = LcdMid with { A = 70 };
        int[]? op = app.ShownOperator switch
        {
            CalculatorApp.Plus => MiniPlus,
            CalculatorApp.Minus => MiniGlyphs[CalculatorApp.NegativeSymbol],
            CalculatorApp.Times => MiniTimes,
            CalculatorApp.Divide => MiniDivide,
            _ => null
        };
        if (op != null) MiniGlyph(screen, 1, 1, op, LcdInk);

        var shown = app.Display;
        int first = CalculatorApp.MaxDigits - shown.Count;
        for (int place = 0; place < CalculatorApp.MaxDigits; place++)
        {
            int col = 5 + place * 4;
            MiniGlyph(screen, col, 1, MiniGlyphs[8], faint);
            if (place >= first) MiniGlyph(screen, col, 1, MiniGlyphs[Math.Clamp(shown[place - first], 0, MiniGlyphs.Length - 1)], LcdInk);
        }
        LcdCells(screen, 1, 7, PoketchAppState.Columns - 2, 1, LcdMid);

        foreach (var key in CalculatorApp.Keys)
        {
            LcdButton(screen, key, app.PressedFor > 0f && app.PressedKey == key.Id);
            string label = key.Id switch
            {
                CalculatorApp.Decimal => ".",
                CalculatorApp.Minus => "-",
                CalculatorApp.Plus => "+",
                CalculatorApp.Times => "×",
                CalculatorApp.Divide => "÷",
                CalculatorApp.EqualsKey => "=",
                CalculatorApp.Clear => "C",
                _ => key.Id.ToString()
            };
            LcdText(screen, label, key.CentreX, key.Y + 1, 28);
        }
    }

    // ------------------------------------------------------------------ the memo pad and the roulette's page

    /// <summary>The page drawn on, a block to a dot, and the rule of the middle tone between it and the tools.</summary>
    private static void PoketchPage(Rectangle screen, Func<int, int, bool> dot)
    {
        for (int y = 0; y < MemoPadApp.Height; y++)
            for (int x = 0; x < MemoPadApp.Width; x++)
                if (dot(x, y)) LcdCells(screen, PoketchCanvas.Left + x, PoketchCanvas.Top + y, 1, 1, LcdInk);
        LcdCells(screen, PoketchCanvas.Left + PoketchCanvas.Width, 1, 1, PoketchAppState.Rows - 2, LcdMid);
    }

    /// <summary>
    /// The memo pad: the page on the left, and at the right the eraser above the pen, the tool in hand shown
    /// pressed in. The eraser is a block of rubber, the pen a stick with a point.
    /// </summary>
    private static void PoketchMemoPad(Rectangle screen, MemoPadApp app, PoketchContext context)
    {
        PoketchPage(screen, app.Dot);

        var eraser = MemoPadApp.EraserButton;
        LcdButton(screen, eraser, !app.PenActive);
        LcdCells(screen, eraser.X + 2, eraser.Y + 3, 2, 7, LcdInk);
        LcdCells(screen, eraser.X + 2, eraser.Y + 7, 2, 3, app.PenActive ? LcdMid : LcdPaper);

        var pen = MemoPadApp.PenButton;
        LcdButton(screen, pen, app.PenActive);
        LcdCells(screen, pen.X + 2, pen.Y + 2, 2, 7, LcdInk);
        LcdCells(screen, pen.X + 2, pen.Y + 9, 2, 1, LcdMid);
        LcdCells(screen, pen.X + 2, pen.Y + 10, 1, 1, LcdInk);
    }

    // ------------------------------------------------------------------ the counter

    /// <summary>The counter: four figures 7 blocks by 13 across the top, and the big button under them.</summary>
    private static void PoketchCounter(Rectangle screen, CounterApp app, PoketchContext context)
    {
        string figures = Math.Clamp(app.Value, 0, CounterApp.Most).ToString("D4");
        for (int i = 0; i < 4; i++) SegmentDigit(screen.X + (7 + i * 8) * Block, screen.Y + 4 * Block, figures[i] - '0');

        var b = CounterApp.Button;
        bool pressed = app.PressedFor > 0f;
        LcdButton(screen, b);
        // The cap: raised on a shadow of ink, or pushed down onto it while pressed
        int sink = pressed ? 1 : 0;
        LcdCells(screen, b.X + 3, b.Y + 9, b.W - 6, 1, LcdInk);
        LcdCells(screen, b.X + 4, b.Y + 2 + sink, b.W - 8, 7, LcdMid);
        LcdCells(screen, b.X + 3, b.Y + 3 + sink, b.W - 6, 5, LcdMid);
        LcdCells(screen, b.X + 5, b.Y + 3 + sink, b.W - 10, 1, pressed ? LcdMid : LcdPaper);
    }

    // ------------------------------------------------------------------ the coin toss

    /// <summary>
    /// The coin toss: a coin 11 blocks across lying in the lower middle, its shadow under it while it flies. In the
    /// air it shows its edge turning; lying, heads (a ball of our own, its top half the middle tone, a band and a
    /// button of ink) or tails (the middle tone with a cross of the paper).
    /// </summary>
    private static void PoketchCoinToss(Rectangle screen, CoinTossApp app, PoketchContext context)
    {
        var coin = CoinTossApp.Coin;
        // The original's pixels to blocks: its screen 160 high is ours 296, at 8 to the block
        int lift = (int)MathF.Round(app.Height * 296f / 160f / Block);
        float cx = coin.CentreX, cy = coin.CentreY - lift;
        const float radius = 5.5f;
        float squash = app.Flying ? Math.Max(0.18f, MathF.Abs(MathF.Cos(app.Frames * 0.55f))) : 1f;

        // The shadow on the ground, narrower the higher the coin
        int shadow = Math.Max(2, 9 - lift / 3);
        LcdCells(screen, (int)MathF.Round(cx - shadow / 2f), coin.Y + coin.H, shadow, 1, LcdMid);

        for (int y = 0; y < coin.H; y++)
            for (int x = 0; x < coin.W; x++)
            {
                float dx = (x + 0.5f - coin.W / 2f) / squash, dy = y + 0.5f - coin.H / 2f;
                float r = MathF.Sqrt(dx * dx + dy * dy);
                if (r > radius) continue;
                Color c;
                if (r > radius - 1.2f || app.Flying) c = r > radius - 1.2f ? LcdInk : LcdMid;
                else if (app.Heads)
                    c = r < 1.3f ? LcdPaper : r < 2.4f ? LcdInk : MathF.Abs(dy) < 0.6f ? LcdInk : dy < 0 ? LcdMid : LcdPaper;
                else
                    c = (MathF.Abs(dx) < 0.6f && MathF.Abs(dy) < 2.6f) || (MathF.Abs(dy) < 0.6f && MathF.Abs(dx) < 2.6f) ? LcdPaper : LcdMid;
                LcdCells(screen, (int)MathF.Round(cx - coin.W / 2f) + x, (int)MathF.Round(cy - coin.H / 2f) + y, 1, 1, c);
            }
    }

    // ------------------------------------------------------------------ the roulette

    /// <summary>
    /// The roulette: the page with the arrow turning over its middle (a shaft of ink 18 blocks long through a hub
    /// 3 across, its head at the long end), and START, STOP and CLEAR down the right as a triangle, a square and a
    /// cross, each shown pressed in while it can't be used.
    /// </summary>
    private static void PoketchRoulette(Rectangle screen, RouletteApp app, PoketchContext context)
    {
        PoketchPage(screen, app.Dot);

        float cx = PoketchCanvas.Left + PoketchCanvas.Width / 2f, cy = PoketchCanvas.Top + PoketchCanvas.Height / 2f;
        float turn = app.Angle / 65536f * MathF.PI * 2f;
        float dx = MathF.Sin(turn), dy = -MathF.Cos(turn);
        void Cell(float x, float y) => LcdCells(screen, (int)MathF.Floor(x), (int)MathF.Floor(y), 1, 1, LcdInk);
        for (float t = -4f; t <= 13f; t += 0.5f) Cell(cx + dx * t, cy + dy * t);
        // The head: two strokes back from the tip, either side of the shaft
        for (float s = 0f; s <= 3.5f; s += 0.5f)
        {
            float bx = cx + dx * (13f - s), by = cy + dy * (13f - s);
            Cell(bx - dy * s * 0.7f, by + dx * s * 0.7f);
            Cell(bx + dy * s * 0.7f, by - dx * s * 0.7f);
        }
        LcdCells(screen, (int)MathF.Floor(cx) - 1, (int)MathF.Floor(cy) - 1, 3, 3, LcdInk);
        LcdCells(screen, (int)MathF.Floor(cx), (int)MathF.Floor(cy), 1, 1, LcdPaper);

        var start = RouletteApp.StartButton;
        LcdButton(screen, start, app.StartDown);
        for (int i = 0; i < 3; i++) LcdCells(screen, start.X + 2 + i, start.Y + 1 + i, 1, 6 - 2 * i, LcdInk);

        var stop = RouletteApp.StopButton;
        LcdButton(screen, stop, app.StopDown);
        LcdCells(screen, stop.X + 2, stop.Y + 3, 2, 2, LcdInk);

        var clear = RouletteApp.ClearButton;
        LcdButton(screen, clear, app.ClearDown);
        for (int i = 0; i < 4; i++)
        {
            LcdCells(screen, clear.X + 1 + i, clear.Y + 2 + i, 1, 1, LcdInk);
            LcdCells(screen, clear.X + 4 - i, clear.Y + 2 + i, 1, 1, LcdInk);
        }
    }

    // ------------------------------------------------------------------ the dot art

    /// <summary>
    /// The dot art: 24 dots by 20, a block each, in the middle of the screen inside a frame of the middle tone, in
    /// four shades: the paper, the middle tone faint, the middle tone and the ink.
    /// </summary>
    private static void PoketchDotArt(Rectangle screen, DotArtApp app, PoketchContext context)
    {
        int left = DotArtApp.Left, top = DotArtApp.Top;
        LcdCells(screen, left - 1, top - 1, DotArtApp.Width + 2, 1, LcdMid);
        LcdCells(screen, left - 1, top + DotArtApp.Height, DotArtApp.Width + 2, 1, LcdMid);
        LcdCells(screen, left - 1, top, 1, DotArtApp.Height, LcdMid);
        LcdCells(screen, left + DotArtApp.Width, top, 1, DotArtApp.Height, LcdMid);
        var faint = LcdMid with { A = 90 };
        for (int y = 0; y < DotArtApp.Height; y++)
            for (int x = 0; x < DotArtApp.Width; x++)
            {
                int shade = app.Dot(x, y);
                if (shade <= 1) continue;
                LcdCells(screen, left + x, top + y, 1, 1, shade == 2 ? faint : shade == 3 ? LcdMid : LcdInk);
            }
    }

    // ------------------------------------------------------------------ the colour changer

    /// <summary>
    /// The colour changer: the colour's name large at the top, and the slider of eight swatches in the lower half,
    /// each its own colour's paper round a square of its ink, a track of ink under them and a knob on it under the
    /// colour chosen.
    /// </summary>
    private static void PoketchColorChanger(Rectangle screen, ColorChangerApp app, PoketchContext context)
    {
        int chosen = Math.Clamp(context.Poketch.ScreenColor, 0, Poketch.ScreenColors - 1);
        LcdText(screen, ColorChangerApp.Names[chosen], PoketchAppState.Columns / 2f, 6, 40);
        LcdText(screen, "SCREEN COLOR", PoketchAppState.Columns / 2f, 13, 24, LcdMid);

        for (int i = 0; i < Poketch.ScreenColors; i++)
        {
            var b = ColorChangerApp.Swatch(i);
            var tones = LcdTones(i);
            LcdCells(screen, b.X, b.Y, b.W, b.H, tones[0]);
            LcdCells(screen, b.X + 1, b.Y + 2, b.W - 2, b.H - 4, tones[1]);
            LcdButton(screen, b);
        }
        var first = ColorChangerApp.Swatch(0);
        var last = ColorChangerApp.Swatch(Poketch.ScreenColors - 1);
        int trackRow = first.Y + first.H + 2;
        LcdCells(screen, first.X + 1, trackRow, last.X + last.W - first.X - 2, 1, LcdInk);
        var on = ColorChangerApp.Swatch(chosen);
        LcdCells(screen, on.X + 1, trackRow - 1, on.W - 2, 3, LcdInk);
        LcdCells(screen, on.X + 1, trackRow, on.W - 2, 1, LcdMid);
    }

    // ------------------------------------------------------------------ the kitchen timer

    /// <summary>
    /// The kitchen timer: minutes and seconds in figures 7 blocks by 13 with a colon, the arrows over and under each
    /// figure while the time is set, a bell at each side that swings while it rings, and START, STOP and RESET
    /// along the bottom, the ones in force shown pressed in.
    /// </summary>
    private static void PoketchKitchenTimer(Rectangle screen, KitchenTimerApp app, PoketchContext context)
    {
        int y = KitchenTimerApp.FigureY;
        for (int i = 0; i < 4; i++) SegmentDigit(screen.X + KitchenTimerApp.FigureX[i] * Block, screen.Y + y * Block, app.Figures[i]);
        LcdCells(screen, 22, y + 3, 1, 1, LcdInk);
        LcdCells(screen, 22, y + 9, 1, 1, LcdInk);

        if (app.State == KitchenTimerApp.Mode.Editing)
            for (int f = 0; f < 4; f++)
            {
                foreach (bool up in new[] { true, false })
                {
                    var a = KitchenTimerApp.Arrow(f, up);
                    var color = app.TouchedFor > 0f && app.Touched == a.Id ? LcdMid : LcdInk;
                    for (int r = 0; r < 3; r++)
                        LcdCells(screen, a.X + 3 - r, up ? a.Y + r : a.Y + 2 - r, 1 + 2 * r, 1, color);
                }
            }

        // The bells: each leans out and back by a block at every beat while it rings
        int lean = app.Ringing ? (app.BellsLeft ? -1 : 1) : 0;
        PoketchBell(screen, 1 + lean, y + 4);
        PoketchBell(screen, 40 - lean, y + 4);

        (PoketchButton Button, string Label, bool Down)[] row =
        {
            (KitchenTimerApp.StartButton, "START", app.StartDown),
            (KitchenTimerApp.StopButton, "STOP", app.StopDown),
            (KitchenTimerApp.ResetButton, "RESET", false),
        };
        foreach (var (b, label, down) in row)
        {
            LcdButton(screen, b, down || (app.TouchedFor > 0f && app.Touched == b.Id));
            LcdText(screen, label, b.CentreX, b.Y + 1, 24);
        }
    }

    /// <summary>A bell 4 blocks by 6: a dome of ink, its mouth a row of the middle tone, the clapper under it.</summary>
    private static void PoketchBell(Rectangle screen, int col, int row)
    {
        LcdCells(screen, col + 1, row, 2, 1, LcdInk);
        LcdCells(screen, col, row + 1, 4, 3, LcdInk);
        LcdCells(screen, col, row + 4, 4, 1, LcdMid);
        LcdCells(screen, col + 1, row + 5, 2, 1, LcdInk);
    }
}
