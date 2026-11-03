using System;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Mauville's roulette on screen (plan 21 · H12, part o; style guide, "Mauville's roulette"): the wheel of twelve slots
/// turning with its ball, Shroomish's shadow and drop or Taillow's flight, and the board of three colours by four Pokémon
/// beside it, with the cursor, the odds, the coins and the balls left. Every piece is drawn with the kit's own shapes.
/// </summary>
internal static partial class ModernUi
{
    private static readonly Color RouletteFelt = new(28, 74, 52, 255), RouletteFeltDeep = new(14, 40, 30, 255);
    private static readonly Color RouletteRed = new(110, 30, 46, 255), RouletteRedDeep = new(60, 14, 24, 255);

    /// <summary>The board's three colours, orange, green and purple.</summary>
    private static readonly Color[] RouletteColours = { new(236, 136, 48, 255), new(80, 176, 92, 255), new(150, 92, 200, 255) };
    /// <summary>A colour of our own for each of the four Pokémon's badges.</summary>
    private static readonly Color[] RoulettePokemon = { new(80, 132, 220, 255), new(120, 180, 240, 255), new(240, 150, 186, 255), new(236, 196, 80, 255) };

    // A Pokémon's badge: a round in its colour with its name's first letter
    private static void RouletteBadge(int pokemon, Vector2 c, float r, float alpha = 1f)
    {
        var col = RoulettePokemon[pokemon];
        UiShapes.Circle(c, r, new Color((int)col.R, col.G, col.B, (int)(255 * alpha)));
        UiShapes.Ring(c, r, Math.Max(2f, r * 0.12f), new Color(255, 255, 255, (int)(200 * alpha)));
        string name = LocalNames.Species(RouletteRound.Columns[pokemon]);
        string letter = name.Length > 0 ? name[..1] : "?";
        float size = r * 1.1f, w = UiFonts.Measure(letter, size, UiWeight.Black);
        UiFonts.DrawCentered(letter, c.X - w / 2f, c.Y, size, new Color(255, 255, 255, (int)(255 * alpha)), UiWeight.Black);
    }

    /// <summary>The table: the wheel at the left, the board at the right, and the table's line under them.</summary>
    public static void DrawRouletteTable(int sw, int sh, RouletteWheel w, float time, float appear)
    {
        bool right = w.RightTable;
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, right ? RouletteFelt : RouletteRed, right ? RouletteFeltDeep : RouletteRedDeep);
        float ease = UiMotion.EaseOut(appear), drop = (1f - ease) * 60f;

        // ---- the wheel: it slides aside for the board while the player bets, and to the middle while the ball rolls
        bool rolling = w.Phase is RoulettePhase.SlideOut or RoulettePhase.Rolling or RoulettePhase.Showing or RoulettePhase.SlideBack;
        float toward = w.Phase switch
        {
            RoulettePhase.SlideOut => Math.Min(1f, w.PhaseFrames / 32f),
            RoulettePhase.SlideBack => 1f - Math.Min(1f, w.PhaseFrames / 32f),
            _ when rolling => 1f,
            _ => 0f
        };
        float scale = 4.2f;
        var centre = new Vector2(sw * (0.3f + 0.2f * UiMotion.EaseInOut(toward)), sh * 0.5f + drop + w.Shake * scale);
        float rim = 92 * scale;
        UiShapes.Shadow(new Rectangle(centre.X - rim, centre.Y - rim, rim * 2, rim * 2), rim, 24, new Vector2(0, 8), ShadowColor);
        UiShapes.Circle(centre, rim, new Color(120, 74, 40, 255));
        UiShapes.Circle(centre, rim - 14, new Color(64, 38, 22, 255));
        // The slots, each a wedge of its square's colour with its Pokémon's badge, turned with the wheel
        for (int slot = 0; slot < RouletteWheel.Slots; slot++)
        {
            int square = RouletteWheel.SquareOfSlot(slot);
            float a0 = (w.WheelAngle + slot * RouletteWheel.DegreesPerSlot) * MathF.PI / 180f;
            float a1 = a0 + RouletteWheel.DegreesPerSlot * MathF.PI / 180f;
            bool hit = w.Board.IsHit(square);
            var col = RouletteColours[square / RouletteRound.Pokemon];
            if (hit) col = Darker(col, 0.45f);
            if (!hit && Lights(w, square) && (int)(time * 4f) % 2 == 0) col = Lighter(col, 0.3f);
            Vector2 At(float a, float r) => centre + new Vector2(MathF.Sin(a), -MathF.Cos(a)) * r;
            float outer = 76 * scale, inner = 40 * scale;
            for (int k = 0; k < 6; k++)
            {
                float b0 = a0 + (a1 - a0) * k / 6f, b1 = a0 + (a1 - a0) * (k + 1) / 6f;
                UiShapes.Triangle(At(b0, inner), At(b0, outer), At(b1, outer), col);
                UiShapes.Triangle(At(b0, inner), At(b1, outer), At(b1, inner), col);
            }
            UiShapes.Line(At(a0, inner), At(a0, outer + 6), 4, new Color(250, 236, 200, 255));
            RouletteBadge(square % RouletteRound.Pokemon, At((a0 + a1) / 2f, 60 * scale), 9 * scale, hit ? 0.45f : 1f);
        }
        UiShapes.Circle(centre, 40 * scale, new Color(200, 160, 90, 255));
        UiShapes.Circle(centre, 30 * scale, new Color(236, 206, 140, 255));
        UiShapes.Ring(centre, 22 * scale, 6, new Color(150, 110, 60, 255));

        // The balls of the round in their slots, then the ball in play
        foreach (int onWheel in w.RestingBalls)
        {
            float a = ((w.WheelAngle + onWheel) % 360) * MathF.PI / 180f;
            RouletteBallAt(centre + new Vector2(MathF.Sin(a), -MathF.Cos(a)) * OnOurWheel(30f) * scale, 5 * scale);
        }
        if (w.Ball is not (RouletteBall.Hidden or RouletteBall.InSlot))
        {
            var (bx, by) = BallOnOurWheel(w);
            float lift = -w.BallLift;
            if (lift > 0) UiShapes.Circle(centre + new Vector2(bx, by + lift) * scale, 4 * scale, new Color(0, 0, 0, 70));
            RouletteBallAt(centre + new Vector2(bx, by) * scale, 5 * scale);
        }
        DrawRouletteHelper(w, centre, scale, time);

        // ---- the board: three rows of colour by four Pokémon, with their heads
        float boardIn = 1f - toward;
        var board = new Rectangle(sw * 0.56f + (1f - boardIn) * sw * 0.5f, 140 + drop, 760, 620);
        if (boardIn > 0.01f)
        {
            Panel(board, 30);
            float cell = 140, x0 = board.X + 30 + 120, y0 = board.Y + 30 + 110;
            for (int c = 0; c < RouletteRound.Pokemon; c++)
            {
                var head = new Vector2(x0 + c * cell + cell / 2f, y0 - 56);
                RouletteBadge(c, head, 40, w.Board.Open(RouletteRound.Kind.Pokemon, 0, c) ? 1f : 0.4f);
            }
            for (int r = 0; r < RouletteRound.Colours; r++)
            {
                var head = new Rectangle(x0 - 110, y0 + r * cell + 20, 96, cell - 40);
                UiShapes.Fill(head, 20, w.Board.Open(RouletteRound.Kind.Colour, r, 0) ? RouletteColours[r] : Darker(RouletteColours[r], 0.5f));
                for (int c = 0; c < RouletteRound.Pokemon; c++)
                {
                    var square = new Rectangle(x0 + c * cell + 8, y0 + r * cell + 8, cell - 16, cell - 16);
                    int index = r * RouletteRound.Pokemon + c;
                    bool hit = w.Board.IsHit(index);
                    UiShapes.Fill(square, 18, hit ? Darker(RouletteColours[r], 0.5f) : Lighter(RouletteColours[r], 0.15f));
                    RouletteBadge(c, new Vector2(square.X + square.Width / 2f, square.Y + square.Height / 2f), 34, hit ? 0.35f : 1f);
                    if (hit) RouletteBallAt(new Vector2(square.X + square.Width - 26, square.Y + 26), 16);
                }
            }
            // The cursor flashes over what is chosen: a column, a row or one square
            if (w.Phase == RoulettePhase.Betting && w.PhaseFrames % 60 < 30)
            {
                int row = w.SelectedRow, col = w.SelectedColumn;
                var sel = row < 0 ? new Rectangle(x0 + col * cell, y0 - 104, cell, cell * 3 + 104)
                    : col < 0 ? new Rectangle(x0 - 118, y0 + row * cell, cell * 4 + 118, cell)
                    : new Rectangle(x0 + col * cell, y0 + row * cell, cell, cell);
                UiShapes.Shape(sel, 22, new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), Gold, 8);
            }
            if (w.Phase == RoulettePhase.Betting)
            {
                string odds = Text.Of("rouletteTable.odds", ("odds", w.SelectedOdds));
                UiFonts.Draw(odds, board.X + 30, board.Y + board.Height - 74, 40, w.SelectedOdds > 0 ? Ink : Muted, UiWeight.Black);
            }
        }

        // ---- the coins, the wager and the balls left
        var side = new Rectangle(48 - (1f - ease) * 200f, 48, 420, 132);
        Panel(side, 26);
        Label(Text.Of("rouletteTable.coins"), side.X + 26, side.Y + 18);
        UiFonts.Draw(w.Coins.ToString("N0"), side.X + 26, side.Y + 50, 48, Ink, UiWeight.Black);
        Label(Text.Of("rouletteTable.balls"), side.X + 250, side.Y + 18);
        for (int i = 0; i < RouletteRound.BallsPerRound; i++)
            RouletteBallAt(new Vector2(side.X + 262 + (i % 3) * 44, side.Y + 70 + (i / 3) * 40), 15, i < RouletteRound.BallsPerRound - w.BallsThisRound ? 1f : 0.25f);
        var wager = new Rectangle(sw - 468 + (1f - ease) * 200f, 48, 420, 80);
        Panel(wager, 26);
        UiFonts.Draw((w.MinBet == 1 ? Text.Of("rouletteTable.wagerOne") : Text.Of("rouletteTable.wager", ("coins", w.MinBet))), wager.X + 26, wager.Y + 24, 32, Ink, UiWeight.Black);

        // ---- the table's line
        string say = w.Phase switch
        {
            RoulettePhase.Betting => Text.Of("rouletteTable.place"),
            RoulettePhase.Result when !w.Won => Text.Of("rouletteTable.miss", ("square", SquareName(w.WinningSquare))),
            RoulettePhase.Result when w.BetOdds == 12 => Text.Of("rouletteTable.jackpot", ("square", SquareName(w.WinningSquare))),
            RoulettePhase.Result => Text.Of("rouletteTable.hit", ("square", SquareName(w.WinningSquare))),
            RoulettePhase.Payout => Text.Of("rouletteTable.won", ("coins", w.MinBet * w.BetOdds)),
            RoulettePhase.Cleared => Text.Of("rouletteTable.cleared"),
            RoulettePhase.MostCoins => Text.Of("rouletteTable.mostCoins"),
            RoulettePhase.Ask => Text.Of("rouletteTable.ask"),
            RoulettePhase.Broke => Text.Of("rouletteTable.broke"),
            _ => ""
        };
        if (say.Length > 0)
        {
            var box = new Rectangle(sw / 2f - 640, sh - 190, 1280, 140);
            Panel(box, 30);
            UiFonts.DrawCentered(say, box.X + 40, box.Y + box.Height / 2f, 38, Ink, UiWeight.Bold);
            if (w.Phase == RoulettePhase.Ask)
            {
                for (int i = 0; i < 2; i++)
                {
                    var pill = new Rectangle(box.X + box.Width - 300 + i * 140, box.Y + 40, 120, 60);
                    bool on = w.AskCursor == i;
                    UiShapes.Shape(pill, 30, on ? Lighter(Selection, 0.14f) : new Color(206, 212, 226, 255), on ? Darker(Selection, 0.06f) : new Color(184, 192, 210, 255),
                        on ? Darker(Selection, 0.3f) : Frame, 3);
                    string word = Text.Of(i == 0 ? "rouletteTable.yes" : "rouletteTable.no");
                    float tw = UiFonts.Measure(word, 28, UiWeight.Black);
                    UiFonts.DrawCentered(word, pill.X + (pill.Width - tw) / 2f, pill.Y + 30, 28, on ? Color.White : Muted, UiWeight.Black);
                }
            }
        }
    }

    // Whether a square is part of the wager the cursor (or the ball in play) is on, for the wheel's slots to flash
    private static bool Lights(RouletteWheel w, int square)
    {
        if (w.Phase == RoulettePhase.Betting)
        {
            int row = w.SelectedRow, col = w.SelectedColumn;
            return row < 0 ? square % RouletteRound.Pokemon == col : col < 0 ? square / RouletteRound.Pokemon == row
                : square == row * RouletteRound.Pokemon + col;
        }
        return false;
    }

    private static string SquareName(int square)
    {
        if (square < 0) return "";
        int colour = square / RouletteRound.Pokemon;
        return Text.Of("roulette.square", ("colour", Text.Of($"roulette.colour.{colour}")),
            ("pokemon", LocalNames.Species(RouletteRound.Columns[square % RouletteRound.Pokemon])));
    }

    /// <summary>
    /// A distance from the wheel's middle in Emerald's pixels as our wheel draws it (plan 21 · part q): Emerald's ball runs at
    /// 68 and rests in a slot at 30, where our slots run from 40 to 76 round a hub of 40, so the ball rested on the hub.
    /// The track is laid on our dark rim and the rest in the slot's inner half.
    /// </summary>
    private static float OnOurWheel(float distance) => 48f + (distance - 30f) * (84f - 48f) / (68f - 30f);

    private static (float X, float Y) BallOnOurWheel(RouletteWheel w)
    {
        float d = w.BallDistance, a = w.BallAngle * MathF.PI / 180f;
        // (BallPosition is the angle and distance with the ball's lift and a slot's shake on its y)
        var (x, y) = w.BallPosition;
        if (d <= 0.01f) return (x, y);
        float k = OnOurWheel(d) / d;
        float baseY = -MathF.Cos(a) * d;
        return (x * k, baseY * k + (y - baseY));
    }

    private static void RouletteBallAt(Vector2 c, float r, float alpha = 1f)
    {
        UiShapes.Circle(c + new Vector2(r * 0.15f, r * 0.2f), r, new Color(0, 0, 0, (int)(80 * alpha)));
        UiShapes.Circle(c, r, new Color(244, 244, 248, (int)(255 * alpha)));
        UiShapes.Circle(c - new Vector2(r * 0.3f, r * 0.3f), r * 0.35f, new Color(255, 255, 255, (int)(255 * alpha)));
    }

    // The helpers as tokens: Shroomish's shadow grows on the wheel, it drops, the wheel shakes and it goes off to the left;
    // Taillow flies in from the side the ball stuck on, lifts the ball and flies away
    private static void DrawRouletteHelper(RouletteWheel w, Vector2 centre, float scale, float time)
    {
        float f = w.HelperFrames;
        if (w.Helper == RouletteHelper.Shroomish)
        {
            var spot = centre + new Vector2(0, (w.StuckOnLeft ? 1 : -1) * 34 * scale);
            switch (w.HelperStep)
            {
                case RouletteHelperStep.Shadow:
                    UiShapes.Circle(spot, Math.Min(1f, f / 30f) * 14 * scale, new Color(0, 0, 0, 70));
                    break;
                case RouletteHelperStep.Falling:
                    UiShapes.Circle(spot, 14 * scale, new Color(0, 0, 0, 90));
                    ShroomishAt(spot - new Vector2(0, (60 - Math.Min(60f, f)) * (60 - Math.Min(60f, f)) * 0.039f * scale), 16 * scale);
                    break;
                case RouletteHelperStep.Shaking:
                    ShroomishAt(spot, 16 * scale);
                    break;
                case RouletteHelperStep.Leaving:
                    ShroomishAt(spot + new Vector2((w.ShroomishLeaving - 36) * scale, 0), 16 * scale);
                    break;
            }
            return;
        }
        if (w.Helper != RouletteHelper.Taillow) return;
        float side = w.StuckOnLeft ? -1f : 1f;
        var (tx, ty) = BallOnOurWheel(w);
        var ball = centre + new Vector2(tx, ty) * scale;
        switch (w.HelperStep)
        {
            case RouletteHelperStep.FlyingIn:
                TaillowAt(ball + new Vector2(side * Math.Max(0f, 120 - f * 2) * scale, -12 * scale), 14 * scale, side, time);
                break;
            case RouletteHelperStep.Carrying:
                TaillowAt(ball + new Vector2(0, -12 * scale), 14 * scale, side, time);
                break;
            case RouletteHelperStep.FlyingAway:
                TaillowAt(ball + new Vector2(-side * f * 1.5f * scale, -(12 + f) * scale), 14 * scale, side, time);
                break;
        }
    }

    // A helper's token: a round of its own colour with the first letter of its name, as the board's badges are
    private static void HelperToken(string species, Vector2 c, float r, Color col)
    {
        UiShapes.Circle(c + new Vector2(r * 0.1f, r * 0.15f), r, new Color(0, 0, 0, 70));
        UiShapes.Circle(c, r, col);
        UiShapes.Ring(c, r, Math.Max(2f, r * 0.12f), new Color(255, 255, 255, 220));
        string name = LocalNames.Species(species);
        string letter = name.Length > 0 ? name[..1] : "?";
        float size = r * 1.1f, w = UiFonts.Measure(letter, size, UiWeight.Black);
        UiFonts.DrawCentered(letter, c.X - w / 2f, c.Y, size, Color.White, UiWeight.Black);
    }

    private static void ShroomishAt(Vector2 c, float r) => HelperToken(RouletteWheel.ShroomishSpecies, c, r, new Color(110, 170, 96, 255));

    // The flier's token, with two plain wings that beat
    private static void TaillowAt(Vector2 c, float r, float facing, float time)
    {
        float beat = MathF.Sin(time * 18f) * r * 0.5f;
        var wing = new Color(60, 80, 140, 255);
        UiShapes.Triangle(c, c + new Vector2(-r * 1.8f, -r * 0.3f - beat), c + new Vector2(-r * 0.5f, r * 0.3f), wing);
        UiShapes.Triangle(c, c + new Vector2(r * 1.8f, -r * 0.3f - beat), c + new Vector2(r * 0.5f, r * 0.3f), wing);
        HelperToken(RouletteWheel.TaillowSpecies,c + new Vector2(facing * r * 0.05f, 0), r * 0.8f, new Color(70, 100, 180, 255));
    }
}
