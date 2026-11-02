using System;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Mauville's slot machines on screen (plan 21 · H12, part n; style guide, "Mauville's slot machines"): the cabinet and its
/// three reels, the lines lit by the coins bet, Pikachu's gauge of sixteen bolts, and the Reel Time machine sliding in
/// over the reels with its number. Every symbol and picture is drawn with the kit's own shapes.
/// </summary>
internal static partial class ModernUi
{
    private static readonly Color ReelNight = new(26, 40, 66, 255), ReelDeep = new(12, 20, 38, 255), Bolt = new(252, 214, 64, 255);

    /// <summary>One of the seven symbols, drawn with the kit's shapes in a cell centred on <paramref name="c"/> (<paramref name="s"/> is the cell's height).</summary>
    public static void ReelSymbolAt(ReelSymbol symbol, Vector2 c, float s, float alpha = 1f)
    {
        Color A(Color col) => new(col.R, col.G, col.B, (byte)(col.A * alpha));
        switch (symbol)
        {
            case ReelSymbol.RedSeven:
                SlotSymbolAt(SlotSymbol.RedSeven, c, s, alpha);
                break;
            case ReelSymbol.BlueSeven:
                SlotSymbolAt(SlotSymbol.BlueSeven, c, s, alpha);
                break;
            case ReelSymbol.Cherry:
                SlotSymbolAt(SlotSymbol.Cherry, c, s, alpha);
                break;
            case ReelSymbol.Replay:
                SlotSymbolAt(SlotSymbol.Replay, c, s, alpha);
                break;
            case ReelSymbol.Azurill:
            {
                // A blue drop with a spotted ball beside it: a badge of our own
                float r = s * 0.26f;
                UiShapes.Circle(new Vector2(c.X - s * 0.08f, c.Y + s * 0.04f), r, A(new Color(92, 150, 232, 255)));
                UiShapes.Circle(new Vector2(c.X - s * 0.16f, c.Y - s * 0.04f), r * 0.28f, A(new Color(196, 222, 255, 255)));
                UiShapes.Circle(new Vector2(c.X + s * 0.24f, c.Y + s * 0.16f), r * 0.5f, A(new Color(70, 120, 210, 255)));
                UiShapes.Circle(new Vector2(c.X + s * 0.24f, c.Y + s * 0.16f), r * 0.22f, A(new Color(220, 236, 255, 255)));
                break;
            }
            case ReelSymbol.Lotad:
            {
                // A lily pad over a green round
                float r = s * 0.24f;
                UiShapes.Circle(new Vector2(c.X, c.Y + s * 0.1f), r, A(new Color(96, 176, 96, 255)));
                UiShapes.Turned(new Vector2(c.X, c.Y - s * 0.12f), s * 0.36f, s * 0.12f, s * 0.12f, 0f, A(new Color(70, 160, 90, 255)));
                UiShapes.Line(new Vector2(c.X, c.Y - s * 0.2f), new Vector2(c.X, c.Y - s * 0.04f), s * 0.03f, A(new Color(40, 110, 60, 255)));
                break;
            }
            case ReelSymbol.Power:
                BoltAt(c, s * 0.7f, A(Bolt), A(new Color(150, 110, 20, 255)));
                break;
        }
    }

    // A lightning bolt, made of two slanted strokes
    private static void BoltAt(Vector2 c, float s, Color fill, Color edge)
    {
        var a = new Vector2(c.X + s * 0.18f, c.Y - s * 0.5f);
        var b = new Vector2(c.X - s * 0.14f, c.Y + s * 0.04f);
        var d = new Vector2(c.X + s * 0.14f, c.Y - s * 0.04f);
        var e = new Vector2(c.X - s * 0.18f, c.Y + s * 0.5f);
        foreach (var (w, col) in new[] { (s * 0.2f, edge), (s * 0.13f, fill) })
        {
            UiShapes.Line(a, b, w, col);
            UiShapes.Line(b, d, w, col);
            UiShapes.Line(d, e, w, col);
        }
    }

    /// <summary>
    /// The machine: the cabinet with its three reels in the middle and the lines the coins light, the table of pays at the
    /// left, the coins, the bet and Pikachu's gauge at the right, and the Reel Time machine sliding in over the reels.
    /// </summary>
    public static void DrawReelMachine(int sw, int sh, ReelMachine m, int number, float time, float appear)
    {
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, ReelNight, ReelDeep);
        float ease = UiMotion.EaseOut(appear), drop = (1f - ease) * 60f;
        const float cellW = 232, cellH = 150, gap = 18;
        var cabinet = new Rectangle(sw / 2f - 450, 96 + drop, 900, 880);
        Panel(cabinet, 36, border: 6, frame: Gold);
        UiFonts.Draw(Text.Of("reelMachine.machine", ("number", number + 1)), cabinet.X + 36, cabinet.Y + 22, 24, Muted, UiWeight.Black);

        // ---- the display over the reels: what to do now
        var display = new Rectangle(cabinet.X + 36, cabinet.Y + 62, cabinet.Width - 72, 190);
        UiShapes.Shape(display, 22, new Color(26, 30, 62, 255), new Color(14, 16, 40, 255), new Color(8, 8, 24, 255), 4);
        string say = m.Notice switch
        {
            ReelNotice.NeedThree => Text.Of("reelMachine.needThree"),
            ReelNotice.MostCoins => Text.Of("reelMachine.mostCoins"),
            ReelNotice.NoCoins => Text.Of("reelMachine.noCoins"),
            ReelNotice.Quit => Text.Of("reelMachine.quit"),
            _ when m.InReelTime => Text.Of("reelMachine.reelTime"),
            _ when m.ReelTimeSpinsLeft > 0 && m.Betting => Text.Of("reelMachine.reelTimeLeft", ("spins", m.ReelTimeSpinsLeft)),
            _ when m.Betting => Text.Of("reelMachine.insert"),
            _ when m.Spinning => Text.Of("reelMachine.stop"),
            _ when m.Paying && (m.Has(ReelMachine.Match.RedSeven) || m.Has(ReelMachine.Match.BlueSeven)) => Text.Of("reelMachine.big"),
            _ when m.Paying && m.Has(ReelMachine.Match.MixedSeven) => Text.Of("reelMachine.regular"),
            _ when m.Paying => Text.Of("reelMachine.win"),
            _ => ""
        };
        float sz = 40, sw2 = UiFonts.Measure(say, sz, UiWeight.Black);
        UiFonts.DrawCentered(say, display.X + (display.Width - sw2) / 2f, display.Y + display.Height / 2f, sz, Bolt, UiWeight.Black);

        // ---- the reels
        float windowW = cellW * 3 + gap * 2;
        var window = new Rectangle(sw / 2f - windowW / 2f, cabinet.Y + 282, windowW, cellH * 3);
        for (int reel = 0; reel < ReelMachine.Reels; reel++)
        {
            var column = new Rectangle(window.X + reel * (cellW + gap), window.Y, cellW, window.Height);
            UiShapes.Fill(column, 14, ReelPaper);
            // The tape's place in symbols: the pixel offset counts down the tape as the reel turns
            int px = m.PixelOf(reel) + m.ShockOf(reel);
            float shift = (px % ReelMachine.SymbolHeight) / (float)ReelMachine.SymbolHeight;
            int top = ReelMachine.SymbolsPerReel - px / ReelMachine.SymbolHeight;
            Raylib.BeginScissorMode((int)(column.X * Core.GameEngine.RenderScale), (int)(column.Y * Core.GameEngine.RenderScale),
                (int)(column.Width * Core.GameEngine.RenderScale), (int)(column.Height * Core.GameEngine.RenderScale));
            for (int k = 0; k <= 4; k++)
            {
                float row = k - 1 + shift;
                if (row <= -1f || row >= 3f) continue;
                int at = ((top + k) % ReelMachine.SymbolsPerReel + ReelMachine.SymbolsPerReel) % ReelMachine.SymbolsPerReel;
                var centre = new Vector2(column.X + cellW / 2f, column.Y + (row + 0.5f) * cellH);
                ReelSymbolAt(ReelMachine.Tapes[reel][at], centre, cellH);
            }
            Raylib.EndScissorMode();
            UiShapes.Shape(column, 14, new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), Frame, 4);
        }
        Raylib.DrawRectangleGradientV((int)window.X, (int)window.Y, (int)window.Width, 46, new Color(20, 22, 50, 130), new Color(20, 22, 50, 0));
        Raylib.DrawRectangleGradientV((int)window.X, (int)(window.Y + window.Height - 46), (int)window.Width, 46, new Color(20, 22, 50, 0), new Color(20, 22, 50, 130));

        // ---- the lines: those the coins light, faint, and those that won, in gold while they flash
        float x0 = window.X - 12, x1 = window.X + window.Width + 12;
        float Y(int row) => window.Y + cellH * (row - 0.5f);
        int lit = m.Bet >= 3 ? 5 : m.Bet == 2 ? 3 : m.Bet == 1 ? 1 : 0;
        for (int line = 0; line < lit; line++)
        {
            var rows = ReelMachine.Lines[line];
            bool won = m.Paying && m.WonLines.Contains(line);
            if (won && (int)(time * 6f) % 2 == 1) continue;
            UiShapes.Line(new Vector2(x0, Y(rows[0])), new Vector2(x1, Y(rows[2])), won ? 8 : 3, won ? new Color(240, 176, 56, 230) : new Color(240, 176, 56, 70));
        }

        // ---- the stop buttons: the next reel to stop is lit
        string[] stops = { Text.Of("reelMachine.button.left"), Text.Of("reelMachine.button.middle"), Text.Of("reelMachine.button.right") };
        for (int reel = 0; reel < ReelMachine.Reels; reel++)
        {
            bool next = m.Spinning && reel == m.CurrentReel;
            var pill = new Rectangle(window.X + reel * (cellW + gap) + 36, window.Y + window.Height + 44, cellW - 72, 56);
            UiShapes.Shadow(pill, 28, 12, new Vector2(0, 4), ShadowColor);
            UiShapes.Shape(pill, 28, next ? Lighter(Selection, 0.14f) : new Color(206, 212, 226, 255), next ? Darker(Selection, 0.06f) : new Color(184, 192, 210, 255),
                next ? Darker(Selection, 0.3f) : Frame, 3);
            float tw = UiFonts.Measure(stops[reel], 24, UiWeight.Black);
            UiFonts.DrawCentered(stops[reel], pill.X + (pill.Width - tw) / 2f, pill.Y + 28, 24, next ? Color.White : Muted, UiWeight.Black);
        }

        // ---- what a line pays
        var pays = new Rectangle(48 - (1f - ease) * 200f, 150, 396, 700);
        Panel(pays, 30);
        Label(Text.Of("reelMachine.pays"), pays.X + 30, pays.Y + 24);
        (ReelSymbol Symbol, int Coins)[] rowsOfPay =
        {
            (ReelSymbol.RedSeven, ReelMachine.Payouts[(int)ReelMachine.Match.RedSeven]), (ReelSymbol.BlueSeven, ReelMachine.Payouts[(int)ReelMachine.Match.BlueSeven]),
            (ReelSymbol.Azurill, ReelMachine.Payouts[(int)ReelMachine.Match.Azurill]), (ReelSymbol.Lotad, ReelMachine.Payouts[(int)ReelMachine.Match.Lotad]),
            (ReelSymbol.Power, ReelMachine.Payouts[(int)ReelMachine.Match.Power]), (ReelSymbol.Cherry, ReelMachine.Payouts[(int)ReelMachine.Match.Cherry]),
            (ReelSymbol.Replay, 0)
        };
        for (int i = 0; i < rowsOfPay.Length; i++)
        {
            float y = pays.Y + 96 + i * 84;
            ReelSymbolAt(rowsOfPay[i].Symbol, new Vector2(pays.X + 96, y), 74);
            string what = rowsOfPay[i].Symbol == ReelSymbol.Replay ? Text.Of("reelMachine.anotherGo") : rowsOfPay[i].Coins.ToString();
            UiFonts.DrawCentered(what, pays.X + 196, y, rowsOfPay[i].Symbol == ReelSymbol.Replay ? 26 : 34, Ink, UiWeight.Black);
        }

        // ---- the coins, the bet and the gauge
        var side = new Rectangle(sw - 444 + (1f - ease) * 200f, 150, 396, 700);
        Panel(side, 30);
        Label(Text.Of("reelMachine.coins"), side.X + 30, side.Y + 24);
        UiFonts.Draw(m.Coins.ToString("N0"), side.X + 30, side.Y + 56, 56, Ink, UiWeight.Black);
        Label(Text.Of("reelMachine.bet"), side.X + 30, side.Y + 150);
        for (int i = 0; i < ReelMachine.MaxBet; i++)
            UiShapes.Circle(new Vector2(side.X + 60 + i * 64, side.Y + 210), 22, i < m.Bet ? Gold : new Color(200, 204, 214, 255));
        Label(Text.Of("reelMachine.payout"), side.X + 30, side.Y + 260);
        UiFonts.Draw(m.Payout.ToString(), side.X + 30, side.Y + 292, 44, Ink, UiWeight.Black);
        Label(Text.Of("reelMachine.gauge"), side.X + 30, side.Y + 380);
        for (int i = 0; i < ReelMachine.MostBolts; i++)
        {
            var at = new Vector2(side.X + 50 + (i % 8) * 44, side.Y + 440 + (i / 8) * 64);
            BoltAt(at, 44, i < m.Bolts ? Bolt : new Color(204, 208, 218, 255), i < m.Bolts ? new Color(150, 110, 20, 255) : new Color(170, 176, 190, 255));
        }
        if (m.LuckyGame) UiFonts.Draw(Text.Of("reelMachine.lucky"), side.X + 30, side.Y + 590, 26, Green, UiWeight.Black);

        // ---- Reel Time: its machine slides in over the reels and turns to the number it drew, or blows up
        int slide = m.ReelTimeWindow;
        if (slide > 0)
        {
            float over = slide / 200f;
            var rt = new Rectangle(window.X + window.Width * (1f - over) * 0.5f, window.Y + 40 + m.ReelTimeShake * 2, window.Width * over, window.Height - 80);
            UiShapes.Shadow(rt, 30, 18, new Vector2(0, 6), ShadowColor);
            UiShapes.Shape(rt, 30, new Color(250, 214, 80, 255), new Color(230, 170, 40, 255), new Color(120, 80, 10, 255), 5);
            if (over >= 1f)
            {
                var face = new Rectangle(rt.X + rt.Width / 2f - 110, rt.Y + 40, 220, rt.Height - 80);
                UiShapes.Fill(face, 18, ReelPaper);
                bool broken = m.ReelTimeStep is ReelTimeStep.Explode or ReelTimeStep.Smoke;
                if (broken)
                {
                    UiShapes.Circle(new Vector2(face.X + face.Width / 2f, face.Y + face.Height / 2f), 70, new Color(90, 90, 100, 200));
                }
                else
                {
                    // The tape of six numbers, twenty pixels each, turning down
                    float h = face.Height / 3f, s2 = (m.ReelTimePixel % 20) / 20f;
                    int topNum = 6 - m.ReelTimePixel / 20;
                    Raylib.BeginScissorMode((int)(face.X * Core.GameEngine.RenderScale), (int)(face.Y * Core.GameEngine.RenderScale),
                        (int)(face.Width * Core.GameEngine.RenderScale), (int)(face.Height * Core.GameEngine.RenderScale));
                    for (int k = 0; k <= 4; k++)
                    {
                        float row = k - 1 + s2;
                        int at = ((topNum + k) % 6 + 6) % 6;
                        string n = ReelMachine.ReelTimeTape[at].ToString();
                        float size = h * 0.8f, w = UiFonts.MeasureDisplay(n, size, 0);
                        UiFonts.DrawDisplay(n, face.X + (face.Width - w) / 2f, face.Y + row * h + h * 0.1f, size, 0, row is > 0.5f and < 1.5f ? SevenRed : Muted);
                    }
                    Raylib.EndScissorMode();
                }
                for (int i = 0; i < 2; i++)
                {
                    float bob = MathF.Sin(time * 8f + i * 3f) * 8f;
                    BoltAt(new Vector2(i == 0 ? rt.X + 90 : rt.X + rt.Width - 90, rt.Y + rt.Height / 2f + bob), 90, Bolt, new Color(150, 110, 20, 255));
                }
            }
        }
    }
}
