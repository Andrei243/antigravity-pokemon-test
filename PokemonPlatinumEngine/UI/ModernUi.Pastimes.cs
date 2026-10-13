using System;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The pastimes of plan 06 · R16b on screen (style guide, "Fossils, the Game Corner, the Villa and Pal Park"): the
/// Game Corner's slot machine, the Coin Case's count over the field, and the Catching Show's count.
/// </summary>
internal static partial class ModernUi
{
    private static readonly Color HallBlue = new(30, 32, 72, 255), HallDeep = new(16, 16, 42, 255), ReelPaper = new(250, 248, 240, 255);
    private static readonly Color SevenRed = new(226, 60, 70, 255), SevenBlue = new(60, 120, 232, 255), MoonYellow = new(250, 220, 110, 255);

    /// <summary>One of the six symbols, drawn with the kit's shapes in a cell centred on <paramref name="c"/> (<paramref name="s"/> is the cell's height).</summary>
    public static void SlotSymbolAt(SlotSymbol symbol, Vector2 c, float s, float alpha = 1f)
    {
        Color A(Color col) => new(col.R, col.G, col.B, (byte)(col.A * alpha));
        switch (symbol)
        {
            case SlotSymbol.RedSeven:
            case SlotSymbol.BlueSeven:
            {
                var ink = symbol == SlotSymbol.RedSeven ? SevenRed : SevenBlue;
                float size = s * 0.92f, width = UiFonts.MeasureDisplay("7", size, 0);
                UiFonts.DrawDisplay("7", c.X - width / 2f + 3, c.Y - size * 0.5f + 3, size, 0, A(new Color(20, 20, 40, 90)));
                UiFonts.DrawDisplay("7", c.X - width / 2f, c.Y - size * 0.5f, size, 0, A(ink));
                break;
            }
            case SlotSymbol.Replay:
            {
                var pill = new Rectangle(c.X - s * 0.62f, c.Y - s * 0.2f, s * 1.24f, s * 0.4f);
                UiShapes.Shape(pill, pill.Height / 2f, A(Lighter(Green, 0.12f)), A(Darker(Green, 0.08f)), A(Darker(Green, 0.35f)), 3);
                float size = s * 0.22f;
                string replay = Text.Of("slots.replay");
                UiFonts.DrawCentered(replay, c.X - UiFonts.Measure(replay, size, UiWeight.Black) / 2f, c.Y, size, A(Color.White), UiWeight.Black);
                break;
            }
            case SlotSymbol.Cherry:
            {
                float r = s * 0.19f;
                var left = new Vector2(c.X - s * 0.2f, c.Y + s * 0.14f);
                var right = new Vector2(c.X + s * 0.2f, c.Y + s * 0.2f);
                var stem = new Vector2(c.X + s * 0.06f, c.Y - s * 0.34f);
                UiShapes.Line(stem, left, s * 0.045f, A(new Color(70, 140, 70, 255)));
                UiShapes.Line(stem, right, s * 0.045f, A(new Color(70, 140, 70, 255)));
                UiShapes.Turned(new Vector2(stem.X + s * 0.1f, stem.Y - s * 0.02f), s * 0.11f, s * 0.05f, s * 0.05f, -24f, A(Green));
                foreach (var at in new[] { left, right })
                {
                    UiShapes.Circle(at, r, A(new Color(206, 40, 56, 255)));
                    UiShapes.Circle(new Vector2(at.X - r * 0.3f, at.Y - r * 0.3f), r * 0.28f, A(new Color(255, 190, 190, 255)));
                }
                break;
            }
            case SlotSymbol.Ball:
            {
                float r = s * 0.33f;
                UiShapes.Circle(c, r, A(Ink));
                UiShapes.Circle(c, r - 4, A(new Color(246, 246, 250, 255)));
                // The upper half red: a half disc made of a disc and the paper drawn back over its lower part
                UiShapes.Shape(new Rectangle(c.X - r + 4, c.Y - r + 4, (r - 4) * 2, r - 4), r - 4, A(SevenRed), A(SevenRed));
                UiShapes.Fill(new Rectangle(c.X - r + 4, c.Y - r * 0.32f, (r - 4) * 2, r * 0.32f), 0, A(SevenRed));
                UiShapes.Fill(new Rectangle(c.X - r + 2, c.Y - 4, r * 2 - 4, 8), 0, A(Ink));
                UiShapes.Circle(c, r * 0.3f, A(Ink));
                UiShapes.Circle(c, r * 0.3f - 4, A(new Color(246, 246, 250, 255)));
                break;
            }
            case SlotSymbol.Moon:
            {
                float r = s * 0.32f;
                UiShapes.Circle(c, r, A(MoonYellow));
                UiShapes.Circle(new Vector2(c.X + r * 0.42f, c.Y - r * 0.16f), r * 0.82f, A(ReelPaper));
                break;
            }
        }
    }

    /// <summary>
    /// The slot machine: the cabinet with its three reels in the middle, the table of pays at the left, the coins at
    /// the right, and over the reels the display where the machine lets on what it drew and Clefairy points the reels
    /// out in the bonus game.
    /// </summary>
    public static void DrawSlotMachine(int sw, int sh, SlotMachine m, int number, float time, float appear)
    {
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, HallBlue, HallDeep);
        // The hall's bulbs along the top, lit in turn
        for (int i = 0; i < 32; i++)
        {
            bool lit = ((int)(time * 4f) + i) % 4 == 0;
            UiShapes.Circle(new Vector2(30 + i * 60, 26), 9, lit ? new Color(255, 226, 130, 255) : new Color(150, 118, 60, 255));
        }
        float ease = UiMotion.EaseOut(appear), drop = (1f - ease) * 60f;
        const float cellW = 232, cellH = 150, gap = 18;
        var cabinet = new Rectangle(sw / 2f - 450, 96 + drop, 900, 880);
        Panel(cabinet, 36, border: 6, frame: Gold);
        UiFonts.Draw(Text.Of("slots.machine", ("number", number + 1)), cabinet.X + 36, cabinet.Y + 22, 24, Muted, UiWeight.Black);

        // ---- the display over the reels
        var display = new Rectangle(cabinet.X + 36, cabinet.Y + 62, cabinet.Width - 72, 190);
        UiShapes.Shape(display, 22, new Color(26, 30, 62, 255), new Color(14, 16, 40, 255), new Color(8, 8, 24, 255), 4);
        DrawSlotDisplay(display, m, time);

        // ---- the reels
        float windowW = cellW * 3 + gap * 2;
        var window = new Rectangle(sw / 2f - windowW / 2f, cabinet.Y + 282, windowW, cellH * 3);
        for (int reel = 0; reel < 3; reel++)
        {
            var column = new Rectangle(window.X + reel * (cellW + gap), window.Y, cellW, window.Height);
            UiShapes.Fill(column, 14, ReelPaper);
            var (above, pixels, bounce) = m.Reel(reel);
            float shift = (pixels + bounce) / SlotMachine.SymbolPixels;
            // What turns past the window's edges is cut off there (a scissor is in the target's own pixels)
            Raylib.BeginScissorMode((int)(column.X * Core.GameEngine.RenderScale), (int)(column.Y * Core.GameEngine.RenderScale),
                (int)(column.Width * Core.GameEngine.RenderScale), (int)(column.Height * Core.GameEngine.RenderScale));
            for (int k = 0; k <= 4; k++)
            {
                float row = k - 1 + shift;
                if (row <= -1f || row >= 3f) continue;
                int at = ((above + k) % SlotMachine.Symbols + SlotMachine.Symbols) % SlotMachine.Symbols;
                var centre = new Vector2(column.X + cellW / 2f, column.Y + (row + 0.5f) * cellH);
                SlotSymbolAt((SlotSymbol)SlotMachine.Reels[reel][at], centre, cellH);
            }
            Raylib.EndScissorMode();
            UiShapes.Shape(column, 14, new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), Frame, 4);
        }
        // The window's top and bottom are in shade
        Raylib.DrawRectangleGradientV((int)window.X, (int)window.Y, (int)window.Width, 46, new Color(20, 22, 50, 130), new Color(20, 22, 50, 0));
        Raylib.DrawRectangleGradientV((int)window.X, (int)(window.Y + window.Height - 46), (int)window.Width, 46, new Color(20, 22, 50, 0), new Color(20, 22, 50, 130));

        // A line that pays, in gold, while its coins are counted
        if (m.Lines != 0 && (m.Counting || m.Payout > 0 || (int)(time * 6f) % 2 == 0))
        {
            float y1 = window.Y + cellH * 0.5f, y2 = window.Y + cellH * 1.5f, y3 = window.Y + cellH * 2.5f, x0 = window.X - 12, x1 = window.X + window.Width + 12;
            var gold = new Color(240, 176, 56, 220);
            if ((m.Lines & 2) != 0) UiShapes.Line(new Vector2(x0, y1), new Vector2(x1, y1), 8, gold);
            if ((m.Lines & 1) != 0) UiShapes.Line(new Vector2(x0, y2), new Vector2(x1, y2), 8, gold);
            if ((m.Lines & 4) != 0) UiShapes.Line(new Vector2(x0, y3), new Vector2(x1, y3), 8, gold);
            if ((m.Lines & 8) != 0) UiShapes.Line(new Vector2(x0, y1), new Vector2(x1, y3), 8, gold);
            if ((m.Lines & 16) != 0) UiShapes.Line(new Vector2(x0, y3), new Vector2(x1, y1), 8, gold);
        }

        // ---- the stop buttons, and in the bonus game the arrow over the reel to stop next
        string[] keys = { Text.Of("slots.button.left"), Text.Of("slots.button.down"), Text.Of("slots.button.right") };
        int next = m.InBonus && m.Spinning && m.Presses < 3 ? SlotMachine.Orders[m.PointedOrder][m.Presses] : -1;
        for (int reel = 0; reel < 3; reel++)
        {
            bool turning = m.Spinning && !m.Pressed(reel);
            var pill = new Rectangle(window.X + reel * (cellW + gap) + 36, window.Y + window.Height + 44, cellW - 72, 56);
            var top = turning ? Lighter(Selection, 0.14f) : new Color(206, 212, 226, 255);
            var bottom = turning ? Darker(Selection, 0.06f) : new Color(184, 192, 210, 255);
            UiShapes.Shadow(pill, 28, 12, new Vector2(0, 4), ShadowColor);
            UiShapes.Shape(pill, 28, top, bottom, turning ? Darker(Selection, 0.3f) : Frame, 3);
            float tw = UiFonts.Measure(keys[reel], 24, UiWeight.Black);
            UiFonts.DrawCentered(keys[reel], pill.X + (pill.Width - tw) / 2f, pill.Y + 28, 24, turning ? Color.White : Muted, UiWeight.Black);
            if (reel == next)
            {
                float bob = MathF.Sin(time * 9f) * 6f;
                UiIcons.ArrowDown(new Vector2(window.X + reel * (cellW + gap) + cellW / 2f, window.Y - 20 + bob * 0.5f), 26, Selection);
            }
        }

        // ---- what each symbol pays
        var pays = new Rectangle(48 - (1f - ease) * 200f, 150, 396, 620);
        Panel(pays, 30);
        Label(m.InBonus ? Text.Of("slots.bonusGame") : Text.Of("slots.pays"), pays.X + 30, pays.Y + 24);
        var table = m.InBonus ? SlotMachine.BonusPays : SlotMachine.Pays;
        SlotSymbol[] order = { SlotSymbol.RedSeven, SlotSymbol.BlueSeven, SlotSymbol.Moon, SlotSymbol.Ball, SlotSymbol.Cherry, SlotSymbol.Replay };
        for (int i = 0; i < order.Length; i++)
        {
            float y = pays.Y + 100 + i * 86;
            bool bonusRow = m.InBonus && order[i] != SlotSymbol.Replay;
            SlotSymbolAt(order[i], new Vector2(pays.X + 96, y), 76, bonusRow ? 0.35f : 1f);
            int coins = table[(int)order[i]];
            string what = order[i] == SlotSymbol.Replay && !m.InBonus ? Text.Of("slots.anotherGo") : order[i] == SlotSymbol.Cherry ? Text.Of("slots.aLine", ("coins", coins)) : coins.ToString();
            UiFonts.DrawCentered(what, pays.X + 196, y, order[i] == SlotSymbol.Replay && !m.InBonus ? 26 : 34, bonusRow ? Muted : Ink, UiWeight.Black);
            if (!m.InBonus && order[i] is SlotSymbol.RedSeven or SlotSymbol.BlueSeven)
                UiFonts.DrawCentered(Text.Of("slots.plusBonus"), pays.X + 272, y, 20, Selection, UiWeight.Black);
        }

        // ---- the coins
        var purse = new Rectangle(sw - 48 - 396 + (1f - ease) * 200f, 150, 396, m.InBonus ? 620 : 400);
        Panel(purse, 30);
        void Count(string label, string value, float y, Color ink)
        {
            Label(label, purse.X + 30, purse.Y + y);
            UiFonts.Draw(value, purse.X + 30, purse.Y + y + 26, 56, ink, UiWeight.Black);
        }
        Count(Text.Of("slots.coins"), m.Coins.ToString("N0"), 24, Ink);
        Count(Text.Of("slots.win"), m.Payout.ToString("N0"), 144, m.Payout > 0 ? Selection : Muted);
        Count(Text.Of("slots.bet"), (m.WaitsForBet ? 0 : m.InBonus ? SlotMachine.BonusBet : SlotMachine.Bet).ToString(), 264, Ink);
        if (m.InBonus)
        {
            Count(Text.Of("slots.round"), m.Rounds.ToString(), 384, Ink);
            Label(Text.Of("slots.gamesLeft"), purse.X + 200, purse.Y + 384);
            UiFonts.Draw(m.GamesLeft.ToString(), purse.X + 200, purse.Y + 410, 56, Ink, UiWeight.Black);
            Count(Text.Of("slots.bonusWon"), m.BonusCoins.ToString("N0"), 504, Gold);
        }

        // ---- the notice a machine stops with, and the keys
        if (m.Notice != SlotNotice.None)
        {
            Dim(sw, sh, 120);
            string text = m.Notice switch
            {
                SlotNotice.NoCoins => Text.Of("slots.notice.noCoins"),
                SlotNotice.TooFewCoins => Text.Of("slots.notice.tooFew"),
                _ => Text.Of("slots.notice.full")
            };
            var box = new Rectangle(sw / 2f - 520, sh / 2f - 80, 1040, 160);
            Panel(box, 34);
            UiFonts.DrawCentered(text, box.X + (box.Width - UiFonts.Measure(text, 40, UiWeight.ExtraBold)) / 2f, box.Y + 80, 40, Ink, UiWeight.ExtraBold);
            AdvanceArrow(box.X + box.Width - 70, box.Y + box.Height - 54);
        }
        string act = m.WaitsForBet ? Text.Of("slots.hints.bet") : m.WaitsForLever ? Text.Of("slots.hints.lever") : m.Counting ? Text.Of("slots.hints.take") : "";
        if (act.Length > 0 && (m.WaitsForBet || m.WaitsForLever))
            Hints(sw - 48, sh - 76, (Text.Of("shared.keys.z"), act), Hint("shared.keys.x", "shared.hints.leave"));
        else if (m.Spinning)
            Hints(sw - 48, sh - 76, Hint("slots.reelKeys", "slots.hints.stop"));
        else if (act.Length > 0)
            Hints(sw - 48, sh - 76, (Text.Of("shared.keys.z"), act));
    }

    // What the display shows: the machine's sign of what it drew; the sevens the bonus waits for; and in the bonus game
    // Clefairy under the moon
    private static void DrawSlotDisplay(Rectangle display, SlotMachine m, float time)
    {
        var centre = new Vector2(display.X + display.Width / 2f, display.Y + display.Height / 2f);
        if (m.InBonus)
        {
            // The moon: plain, red (winning this game tires Clefairy) or ringed with light (another round is certain)
            var moon = new Vector2(display.X + display.Width - 120, display.Y + 76);
            var colour = m.Moon == SlotMoon.Red ? new Color(232, 84, 76, 255) : MoonYellow;
            if (m.Moon == SlotMoon.Bright)
            {
                float pulse = 0.5f + 0.5f * MathF.Sin(time * 6f);
                UiShapes.Glow(moon, 110 + pulse * 16, 110 + pulse * 16, new Color(255, 244, 190, 120));
                UiShapes.Ring(moon, 64 + pulse * 6, 5, new Color(255, 250, 220, 220));
            }
            UiShapes.Circle(moon, 52, colour);
            UiShapes.Circle(new Vector2(moon.X - 16, moon.Y - 12), 9, Darker(colour, 0.12f));
            UiShapes.Circle(new Vector2(moon.X + 18, moon.Y + 14), 13, Darker(colour, 0.12f));
            // A few stars
            for (int i = 0; i < 9; i++)
            {
                var star = new Vector2(display.X + 40 + i * 71 % (display.Width - 240), display.Y + 26 + i * 37 % 120);
                float twinkle = 0.5f + 0.5f * MathF.Sin(time * 3f + i * 1.7f);
                UiShapes.Circle(star, 2.5f + twinkle * 1.5f, new Color(255, 250, 220, (int)(140 + twinkle * 100)));
            }
            // Clefairy, hopping while the reels turn
            var sprite = PixelArtGenerator.GetPokemonSprite("Clefairy", false);
            float hop = m.Spinning ? MathF.Abs(MathF.Sin(time * 7f)) * 12f : 0f;
            float size = 168;
            Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
                new Rectangle(display.X + 70, display.Y + display.Height - size - 4 - hop, size, size), Vector2.Zero, 0f, Color.White);
            string word = m.Moon == SlotMoon.Red ? Text.Of("slots.moon.red") : m.Moon == SlotMoon.Bright ? Text.Of("slots.moon.bright") : m.Spinning ? Text.Of("slots.pointing") : "";
            if (word.Length > 0) UiFonts.DrawCentered(word, display.X + 270, display.Y + 44, 28, Color.White, UiWeight.ExtraBold);
            return;
        }
        if (m.Awaited is { } sevens)
        {
            // The bonus is won and waits: three sevens to line up
            float pulse = 0.75f + 0.25f * MathF.Sin(time * 8f);
            for (int i = -1; i <= 1; i++) SlotSymbolAt(sevens, new Vector2(centre.X + i * 130, centre.Y + 6), 150, pulse);
            UiFonts.DrawCentered(Text.Of("slots.lineThemUp"), display.X + 30, display.Y + 30, 24, Gold, UiWeight.Black);
            return;
        }
        if (m.Tell is { } tell)
        {
            // A sign of what was drawn: the symbol itself most of the time, a question mark otherwise
            UiShapes.Circle(centre, 78, new Color(60, 70, 132, 255));
            UiShapes.Ring(centre, 78, 5, Gold);
            if (tell == SlotSymbol.None)
            {
                float width = UiFonts.MeasureDisplay("?", 110, 0);
                UiFonts.DrawDisplay("?", centre.X - width / 2f, centre.Y - 58, 110, 0, Gold);
            }
            else
            {
                UiShapes.Circle(centre, 68, ReelPaper);
                SlotSymbolAt(tell, centre, 120);
            }
        }
    }

    /// <summary>
    /// The coins in the Coin Case and the money beside them, on a panel at the top left while someone of the Game
    /// Corner or its prize exchange is spoken to (the original's two windows there).
    /// </summary>
    public static void CoinCount(int coins, int money)
    {
        var r = new Rectangle(32, 32, 440, 96);
        Panel(r, 24);
        Label(Text.Of("pastimes.coins"), r.X + 28, r.Y + 18);
        UiFonts.Draw(coins.ToString("N0"), r.X + 28, r.Y + 44, 36, Ink, UiWeight.Black);
        Label(Text.Of("pastimes.money"), r.X + 212, r.Y + 18);
        Money(r.X + 212, r.Y + 64, money, 36, Ink);
    }

    /// <summary>The Catching Show's count: the Park Balls left and the seconds run, where the Safari Game's count stands.</summary>
    public static void ParkCount(int sw, int balls, int seconds)
    {
        var r = new Rectangle(sw - 32 - 300, 32, 300, 96);
        Panel(r, 24);
        Label(Text.Of("pastimes.parkBalls"), r.X + 28, r.Y + 18);
        UiFonts.Draw(balls.ToString(), r.X + 28, r.Y + 44, 36, Ink, UiWeight.Black);
        Label(Text.Of("pastimes.time"), r.X + 184, r.Y + 18);
        UiFonts.Draw($"{seconds / 60}:{seconds % 60:D2}", r.X + 184, r.Y + 44, 36, Ink, UiWeight.Black);
    }
}
