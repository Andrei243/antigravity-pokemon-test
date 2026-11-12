using System;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The card-flip table on screen (plan 42 · PS3; style guide, "Fossils, the Game Corner, the Villa and Pal Park", "The
/// card-flip game"). Every piece is ours. The rectangles are worked out by plain methods, which the screen's pointer asks
/// too, so a click finds what is drawn.
/// </summary>
internal static partial class ModernUi
{
    private static readonly Color FlipFeltTop = new(24, 92, 96, 255), FlipFeltBottom = new(12, 54, 60, 255);
    private static readonly Color FlipCardBack = new(246, 240, 224, 255), FlipCardPaper = new(252, 252, 248, 255);
    private static readonly Color FlipBustSlate = new(70, 76, 92, 255), FlipCrackLight = new(255, 236, 170, 255);
    private static readonly Color FlipCoral = new(232, 120, 96, 255), FlipGreen = new(96, 176, 104, 255), FlipGold = new(236, 180, 64, 255),
        FlipBlue = new(80, 140, 226, 255), FlipViolet = new(156, 104, 214, 255), FlipSlate = new(120, 132, 156, 255);

    /// <summary>A card's side, the space between, and the table's top left.</summary>
    public const float FlipCardSize = 132, FlipCardGap = 14, FlipTableLeft = 96, FlipTableTop = 150;
    public const float FlipPanelLeft = 1040, FlipPanelWidth = 816;

    private static readonly Color[] FlipLineColours = { FlipCoral, FlipGreen, FlipGold, FlipBlue, FlipViolet };

    /// <summary>The colour of a memo's mark (0 a bust, 1 to 3) and of a turned number's band.</summary>
    public static Color CardFlipMarkColour(int mark) => mark switch { 0 => FlipCoral, 1 => FlipSlate, 2 => FlipGreen, _ => FlipGold };

    /// <summary>Where card <paramref name="card"/> stands (0 to 24, along each row from the top left).</summary>
    public static Rectangle CardFlipCardRect(int card)
    {
        int row = card / CardFlipBoard.Side, column = card % CardFlipBoard.Side;
        const float pitch = FlipCardSize + FlipCardGap;
        return new Rectangle(FlipTableLeft + column * pitch, FlipTableTop + row * pitch, FlipCardSize, FlipCardSize);
    }

    /// <summary>The clue tile at a row's right end (<paramref name="row"/> true) or under a column's foot.</summary>
    public static Rectangle CardFlipClueRect(bool row, int i)
    {
        const float pitch = FlipCardSize + FlipCardGap;
        return row
            ? new Rectangle(FlipTableLeft + CardFlipBoard.Side * pitch, FlipTableTop + i * pitch, FlipCardSize, FlipCardSize)
            : new Rectangle(FlipTableLeft + i * pitch, FlipTableTop + CardFlipBoard.Side * pitch, FlipCardSize, FlipCardSize);
    }

    /// <summary>The panel at the right.</summary>
    public static Rectangle CardFlipPanelRect() => new(FlipPanelLeft, FlipTableTop, FlipPanelWidth, 640);

    /// <summary>The memo's button for mark <paramref name="mark"/> (0 a bust, 1 to 3).</summary>
    public static Rectangle CardFlipMarkRect(int mark)
    {
        var p = CardFlipPanelRect();
        const float w = 168, gap = 20;
        return new Rectangle(p.X + 40 + mark * (w + gap), p.Y + 412, w, 84);
    }

    /// <summary>The QUIT button.</summary>
    public static Rectangle CardFlipQuitRect()
    {
        var p = CardFlipPanelRect();
        return new Rectangle(p.X + p.Width - 40 - 260, p.Y + p.Height - 116, 260, 84);
    }

    /// <summary>The strip under the panel where the table's line stands.</summary>
    public static Rectangle CardFlipLineRect()
    {
        var p = CardFlipPanelRect();
        return new Rectangle(p.X, p.Y + p.Height + 30, p.Width, 120);
    }

    public static void DrawCardFlip(int sw, int sh, CardFlipScreen screen, float appear)
    {
        var game = screen.Game;
        if (game == null) return;
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, HallBlue, HallDeep);
        for (int i = 0; i < 32; i++)
        {
            bool lit = ((int)(screen.Age * 4f) + i) % 4 == 0;
            UiShapes.Circle(new Vector2(30 + i * 60, 26), 9, lit ? new Color(255, 226, 130, 255) : new Color(150, 118, 60, 255));
        }
        float drop = (1f - UiMotion.EaseOut(appear)) * 60f;

        // ---- the felt and the board
        const float pitch = FlipCardSize + FlipCardGap;
        var felt = new Rectangle(FlipTableLeft - 28, FlipTableTop - 28 + drop, pitch * 6 - FlipCardGap + 56, pitch * 6 - FlipCardGap + 56);
        UiShapes.Shadow(felt, 30, 26, new Vector2(0, 10), ShadowColor);
        UiShapes.Shape(felt, 30, FlipFeltTop, FlipFeltBottom, Darker(FlipFeltBottom, 0.3f), 5);
        var board = game.Board;
        for (int i = 0; i < CardFlipBoard.Cards; i++)
        {
            var r = FlipMoved(CardFlipCardRect(i), drop);
            if (board == null) { EmptySlot(r, 14); continue; }
            FlipCardAt(r, screen, board, i);
        }
        for (int i = 0; i < CardFlipBoard.Side; i++)
        {
            FlipClueTile(FlipMoved(CardFlipClueRect(true, i), drop), FlipLineColours[i], board?.PointsInRow(i), board?.BustsInRow(i));
            FlipClueTile(FlipMoved(CardFlipClueRect(false, i), drop), FlipLineColours[i], board?.PointsInColumn(i), board?.BustsInColumn(i));
        }
        if (screen.Phase == CardFlipPhase.Board)
        {
            var c = FlipMoved(CardFlipCardRect(screen.Cursor), drop);
            UiShapes.Shape(new Rectangle(c.X - 8, c.Y - 8, c.Width + 16, c.Height + 16), 18, Color.Blank, Color.Blank, Gold, 6);
        }

        // ---- the panel
        var p = FlipMoved(CardFlipPanelRect(), drop);
        Panel(p, 30, border: 6, frame: Gold);
        Label(Text.Of("cardFlip.level"), p.X + 40, p.Y + 32);
        UiFonts.DrawDisplay(game.ShownLevel.ToString(), p.X + 40, p.Y + 58, 96, 0, Ink);
        Label(Text.Of("cardFlip.coins"), p.X + 300, p.Y + 32);
        UiFonts.Draw(game.Coins.ToString("N0"), p.X + 300, p.Y + 62, 44, Ink, UiWeight.Black);
        Label(Text.Of("cardFlip.round"), p.X + 300, p.Y + 140);
        int stake = game.Outcome == CardFlipOutcome.Lost ? 0 : game.Payout;
        UiFonts.Draw(stake.ToString("N0"), p.X + 300, p.Y + 170, 44, Darker(Gold, 0.2f), UiWeight.Black);
        Label(Text.Of("cardFlip.memo"), p.X + 40, p.Y + 372);
        for (int m = 0; m < CardFlipGame.Marks; m++)
        {
            var r = FlipMoved(CardFlipMarkRect(m), drop);
            bool on = board != null && screen.Phase == CardFlipPhase.Board && board.HasMark(screen.Cursor, m);
            var colour = CardFlipMarkColour(m);
            Button(r, 30, on ? colour : Lighter(colour, 0.55f), FlipMarkLabel(m), 30, m == screen.MemoMark && screen.Phase == CardFlipPhase.Board);
        }
        Button(FlipMoved(CardFlipQuitRect(), drop), 42, Red, Text.Of("cardFlip.quit"), 32, false);
        Hints(sw - Margin, 1040, Hint("shared.keys.z", "cardFlip.hint.flip"), Hint("shared.keys.c", "cardFlip.hint.memo"),
            Hint("shared.keys.esc", "cardFlip.hint.quit"));

        // ---- the line
        var line = FlipMoved(CardFlipLineRect(), drop);
        UiShapes.Fill(line, 24, new Color(14, 16, 40, 230));
        DrawWrapped(screen.Line, line.X + 32, line.Y + 22, line.Width - 64, 30, Color.White, 38, UiWeight.ExtraBold);

        // ---- the questions over it
        switch (screen.Phase)
        {
            case CardFlipPhase.Menu:
            {
                string body = screen.LevelChange > 0 ? Text.Of("cardFlip.ask.up")
                    : screen.LevelChange < 0 ? Text.Of("cardFlip.ask.down") : Text.Of("cardFlip.ask.body");
                Prompt(sw, sh, Text.Of("cardFlip.ask.title", ("level", game.ShownLevel)), body,
                    new[] { (Text.Of("cardFlip.ask.play"), Green), (Text.Of("cardFlip.ask.leave"), Blue) }, screen.Choice);
                break;
            }
            case CardFlipPhase.ConfirmQuit:
                Prompt(sw, sh, Text.Of("cardFlip.quitAsk.title"),
                    game.Payout > 0 ? Text.Of("cardFlip.quitAsk.keep", ("coins", game.Payout)) : Text.Of("cardFlip.quitAsk.none"),
                    new[] { (Text.Of("cardFlip.quitAsk.yes"), Red), (Text.Of("cardFlip.quitAsk.no"), Blue) }, screen.Choice);
                break;
        }
    }

    private static Rectangle FlipMoved(Rectangle r, float dy) => new(r.X, r.Y + dy, r.Width, r.Height);

    private static string FlipMarkLabel(int mark) => mark == 0 ? Text.Of("cardFlip.mark.bust") : mark.ToString();

    /// <summary>A card: its back face down, its face up, turning over by its width while <see cref="CardFlipScreen.FlipTime"/> runs.</summary>
    private static void FlipCardAt(Rectangle r, CardFlipScreen screen, CardFlipBoard board, int card)
    {
        bool up = board.IsFlipped(card);
        float turn = 1f;
        bool turning = screen.Phase == CardFlipPhase.Reveal && !screen.UpBefore[card] || card == screen.LastFlipped;
        if (up && turning && screen.FlipAge < CardFlipScreen.FlipTime)
        {
            float t = screen.FlipAge / CardFlipScreen.FlipTime;
            turn = MathF.Abs(MathF.Cos(t * MathF.PI));
            up = t >= 0.5f;
        }
        float w = MathF.Max(4f, r.Width * turn);
        var face = new Rectangle(r.X + (r.Width - w) / 2f, r.Y, w, r.Height);
        UiShapes.Shadow(face, 14, 10, new Vector2(0, 5), new Color(4, 20, 24, 140));
        if (!up)
        {
            UiShapes.Shape(face, 14, FlipCardBack, Darker(FlipCardBack, 0.08f), Gold, 4);
            if (turn > 0.6f)
            {
                var c = new Vector2(face.X + face.Width / 2f, face.Y + face.Height / 2f);
                for (int k = -1; k <= 1; k++)
                    UiShapes.Turned(c + new Vector2(0, k * 30), 14 * turn, 14, 3, 45f, Lighter(Gold, 0.1f));
                for (int m = 0; m < CardFlipGame.Marks; m++)
                    if (board.HasMark(card, m)) FlipMemoPip(face, m);
            }
            return;
        }
        var shown = board.Face(card);
        if (shown == CardFace.Bust)
        {
            UiShapes.Shape(face, 14, Lighter(FlipBustSlate, 0.1f), FlipBustSlate, Darker(FlipBustSlate, 0.4f), 4);
            if (turn > 0.6f)
            {
                // A jagged crack of light from corner to corner, two chips beside it
                var a = new Vector2(face.X + face.Width * 0.18f, face.Y + face.Height * 0.16f);
                var b = new Vector2(face.X + face.Width * 0.46f, face.Y + face.Height * 0.44f);
                var c = new Vector2(face.X + face.Width * 0.38f, face.Y + face.Height * 0.58f);
                var d = new Vector2(face.X + face.Width * 0.82f, face.Y + face.Height * 0.86f);
                UiShapes.Line(a, b, 7, FlipCrackLight);
                UiShapes.Line(b, c, 7, FlipCrackLight);
                UiShapes.Line(c, d, 7, FlipCrackLight);
                UiShapes.Triangle(new Vector2(face.X + face.Width * 0.66f, face.Y + face.Height * 0.2f),
                    new Vector2(face.X + face.Width * 0.78f, face.Y + face.Height * 0.26f),
                    new Vector2(face.X + face.Width * 0.68f, face.Y + face.Height * 0.34f), Darker(FlipBustSlate, 0.35f));
                UiShapes.Triangle(new Vector2(face.X + face.Width * 0.2f, face.Y + face.Height * 0.7f),
                    new Vector2(face.X + face.Width * 0.3f, face.Y + face.Height * 0.74f),
                    new Vector2(face.X + face.Width * 0.22f, face.Y + face.Height * 0.82f), Darker(FlipBustSlate, 0.35f));
            }
            return;
        }
        int value = CardFlipBoard.ValueOf(shown);
        UiShapes.Shape(face, 14, FlipCardPaper, Darker(FlipCardPaper, 0.05f), Darker(FlipMarkFor(value), 0.25f), 4);
        UiShapes.Fill(new Rectangle(face.X + 4, face.Y + 4, face.Width - 8, 18), 10, FlipMarkFor(value));
        if (turn > 0.6f)
        {
            string digit = value.ToString();
            float size = 84 * turn, width = UiFonts.MeasureDisplay(digit, size, 0);
            UiFonts.DrawDisplay(digit, face.X + face.Width / 2f - width / 2f, face.Y + face.Height / 2f - size * 0.42f, size, 0, Ink);
        }
    }

    private static Color FlipMarkFor(int value) => value switch { 1 => FlipSlate, 2 => FlipGreen, _ => FlipGold };

    /// <summary>A memo's pip in its corner of a card face down: a crack for a bust at the top left, then 1, 2 and 3.</summary>
    private static void FlipMemoPip(Rectangle card, int mark)
    {
        float x = mark is 0 or 2 ? card.X + 24 : card.X + card.Width - 24;
        float y = mark is 0 or 1 ? card.Y + 24 : card.Y + card.Height - 24;
        var c = new Vector2(x, y);
        var colour = CardFlipMarkColour(mark);
        UiShapes.Circle(c, 17, colour);
        if (mark == 0)
        {
            UiShapes.Line(c + new Vector2(-8, -8), c + new Vector2(0, 1), 4, Color.White);
            UiShapes.Line(c + new Vector2(0, 1), c + new Vector2(8, 8), 4, Color.White);
        }
        else
        {
            string digit = mark.ToString();
            UiFonts.DrawCentered(digit, c.X - UiFonts.Measure(digit, 24, UiWeight.Black) / 2f, c.Y, 24, Color.White, UiWeight.Black);
        }
    }

    /// <summary>A line's clue: its sum large over a small cracked tile and its busts beside it.</summary>
    private static void FlipClueTile(Rectangle r, Color colour, int? points, int? busts)
    {
        UiShapes.Shape(r, 14, Lighter(colour, 0.15f), Darker(colour, 0.1f), Darker(colour, 0.35f), 4);
        string sum = points?.ToString() ?? "-";
        UiFonts.DrawCentered(sum, r.X + r.Width / 2f - UiFonts.Measure(sum, 40, UiWeight.Black) / 2f, r.Y + 36, 40, Color.White, UiWeight.Black);
        UiShapes.Fill(new Rectangle(r.X + 14, r.Y + r.Height / 2f + 4, r.Width - 28, 4), 2, Darker(colour, 0.25f));
        var tile = new Rectangle(r.X + 26, r.Y + r.Height - 54, 36, 36);
        UiShapes.Fill(tile, 8, FlipBustSlate);
        UiShapes.Line(new Vector2(tile.X + 8, tile.Y + 7), new Vector2(tile.X + 18, tile.Y + 19), 3, FlipCrackLight);
        UiShapes.Line(new Vector2(tile.X + 18, tile.Y + 19), new Vector2(tile.X + 28, tile.Y + 29), 3, FlipCrackLight);
        string count = busts?.ToString() ?? "-";
        UiFonts.DrawCentered(count, r.X + 82, r.Y + r.Height - 36, 32, Color.White, UiWeight.Black);
    }
}
